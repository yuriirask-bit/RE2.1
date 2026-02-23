<#
.SYNOPSIS
    Provisions all RE2 Compliance Platform custom entities in a Dataverse environment.

.DESCRIPTION
    Reads entity definitions from dataverse-schema.json and creates them via the
    Dataverse Web API. Idempotent — skips entities/columns that already exist.
    After provisioning, creates an unmanaged solution containing all entities.

.PARAMETER DataverseUrl
    The Dataverse environment URL (e.g., https://org12345.crm4.dynamics.com).

.PARAMETER TenantId
    Azure AD tenant ID for SPN authentication.

.PARAMETER ClientId
    Azure AD app registration client ID.

.PARAMETER ClientSecret
    Azure AD app registration client secret. If not provided, uses az CLI token (for pipeline Managed Identity).

.PARAMETER ResourceGroupName
    Azure resource group containing App Services/Function Apps whose managed identities
    should be registered as Dataverse Application Users. Optional.

.PARAMETER AppNames
    Comma-separated list of App Service and Function App names whose managed identities
    should be registered as Dataverse Application Users (e.g., "app-re2-api-{env},app-re2-web-{env},func-re2-compliance-{env}").
    Requires ResourceGroupName. The pipeline service connection needs Directory.Read.All
    permission in Azure AD to resolve managed identity Application (Client) IDs.

.PARAMETER DataverseRoleName
    Security role to assign to registered Application Users. Default: "System Administrator".

.EXAMPLE
    # SPN auth (CI/CD pipeline):
    ./provision-dataverse-schema.ps1 -DataverseUrl "https://org.crm4.dynamics.com" -TenantId "xxx" -ClientId "yyy" -ClientSecret "zzz"

    # az CLI auth (pipeline with AzureCLI@2 task):
    ./provision-dataverse-schema.ps1 -DataverseUrl "https://org.crm4.dynamics.com"

    # With managed identity registration:
    ./provision-dataverse-schema.ps1 -DataverseUrl "https://org.crm4.dynamics.com" -ResourceGroupName "rg-re2-{env}" -AppNames "app-re2-api-{env},app-re2-web-{env},func-re2-compliance-{env}"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DataverseUrl,

    [Parameter(Mandatory = $false)]
    [string]$TenantId,

    [Parameter(Mandatory = $false)]
    [string]$ClientId,

    [Parameter(Mandatory = $false)]
    [string]$ClientSecret,

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$AppNames,

    [Parameter(Mandatory = $false)]
    [string]$DataverseRoleName = "System Administrator"
)

$ErrorActionPreference = 'Stop'

# --- Helpers ---

function Get-DataverseToken {
    param([string]$DataverseUrl, [string]$TenantId, [string]$ClientId, [string]$ClientSecret)

    $resource = $DataverseUrl.TrimEnd('/')

    if ($ClientSecret) {
        Write-Host "Authenticating via SPN (client credentials)..."
        $tokenUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
        $body = @{
            grant_type    = "client_credentials"
            client_id     = $ClientId
            client_secret = $ClientSecret
            scope         = "$resource/.default"
        }
        $response = Invoke-RestMethod -Uri $tokenUrl -Method POST -Body $body -ContentType "application/x-www-form-urlencoded"
        return $response.access_token
    }
    else {
        Write-Host "Authenticating via az CLI token (Managed Identity / pipeline)..."
        $token = az account get-access-token --resource $resource --query accessToken -o tsv
        if ($LASTEXITCODE -ne 0) { throw "Failed to acquire token via az CLI" }
        return $token
    }
}

function Invoke-DataverseApi {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body,
        [hashtable]$Headers
    )

    $params = @{
        Method  = $Method
        Uri     = $Uri
        Headers = $Headers
    }
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
        $params.ContentType = "application/json"
    }

    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $detail = $_.ErrorDetails.Message
        if ($detail) {
            try { $detail = ($detail | ConvertFrom-Json).error.message } catch {}
        }
        return @{ _error = $true; StatusCode = $statusCode; Message = $detail }
    }
}

function Get-DataverseTypeMapping {
    param([string]$SchemaType)

    switch ($SchemaType) {
        "String"           { return @{ AttributeType = "StringType";    "@odata.type" = "Microsoft.Dynamics.CRM.StringAttributeMetadata" } }
        "Memo"             { return @{ AttributeType = "MemoType";      "@odata.type" = "Microsoft.Dynamics.CRM.MemoAttributeMetadata" } }
        "Integer"          { return @{ AttributeType = "IntegerType";   "@odata.type" = "Microsoft.Dynamics.CRM.IntegerAttributeMetadata" } }
        "BigInt"           { return @{ AttributeType = "BigIntType";    "@odata.type" = "Microsoft.Dynamics.CRM.BigIntAttributeMetadata" } }
        "Decimal"          { return @{ AttributeType = "DecimalType";   "@odata.type" = "Microsoft.Dynamics.CRM.DecimalAttributeMetadata" } }
        "Boolean"          { return @{ AttributeType = "BooleanType";   "@odata.type" = "Microsoft.Dynamics.CRM.BooleanAttributeMetadata" } }
        "DateTime"         { return @{ AttributeType = "DateTimeType";  "@odata.type" = "Microsoft.Dynamics.CRM.DateTimeAttributeMetadata" } }
        "Uniqueidentifier" { return @{ AttributeType = "StringType"; "@odata.type" = "Microsoft.Dynamics.CRM.StringAttributeMetadata"; _isGuidString = $true } }
        "Picklist"         { return @{ AttributeType = "PicklistType";  "@odata.type" = "Microsoft.Dynamics.CRM.PicklistAttributeMetadata" } }
        "Lookup"           { return @{ AttributeType = "LookupType";    "@odata.type" = "Microsoft.Dynamics.CRM.LookupAttributeMetadata" } }
        default            { throw "Unknown schema type: $SchemaType" }
    }
}

# --- Main ---

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$schemaFile = Join-Path $scriptDir "dataverse-schema.json"
if (-not (Test-Path $schemaFile)) {
    throw "Schema file not found: $schemaFile"
}

$schema = Get-Content $schemaFile -Raw | ConvertFrom-Json
$publisherPrefix = $schema.publisher.prefix
$solutionName = $schema.publisher.solutionUniqueName
$solutionDisplayName = $schema.publisher.solutionDisplayName
$solutionVersion = $schema.publisher.solutionVersion

Write-Host "=== RE2 Dataverse Schema Provisioning ==="
Write-Host "Dataverse URL: $DataverseUrl"
Write-Host "Entities to provision: $($schema.entities.Count)"
Write-Host ""

# Authenticate
$token = Get-DataverseToken -DataverseUrl $DataverseUrl -TenantId $TenantId -ClientId $ClientId -ClientSecret $ClientSecret
$baseUrl = "$($DataverseUrl.TrimEnd('/'))/api/data/v9.2"
$headers = @{
    Authorization  = "Bearer $token"
    "OData-MaxVersion" = "4.0"
    "OData-Version"    = "4.0"
    Accept             = "application/json"
}

function Add-ColumnToEntity {
    param(
        [string]$EntityLogicalName,
        [object]$Column,
        [string]$BaseUrl,
        [hashtable]$Headers
    )

    $typeMapping = Get-DataverseTypeMapping -SchemaType $Column.type
    $attrPayload = @{
        "@odata.type" = $typeMapping."@odata.type"
        SchemaName    = $Column.logicalName
        DisplayName   = @{
            "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
            LocalizedLabels = @(
                @{
                    "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                    Label         = $Column.displayName
                    LanguageCode  = 1033
                }
            )
        }
        RequiredLevel = @{ Value = "None" }
    }

    # Uniqueidentifier columns can't be created via SDK — store as String(36) instead
    if ($Column.type -eq "Uniqueidentifier") {
        $attrPayload."@odata.type" = "Microsoft.Dynamics.CRM.StringAttributeMetadata"
        $attrPayload.MaxLength = 36
        $attrPayload.FormatName = @{ Value = "Text" }
        $attrPayload.Description = @{
            "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
            LocalizedLabels = @(@{ "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"; Label = "GUID stored as string (Dataverse SDK limitation)"; LanguageCode = 1033 })
        }
        Write-Host "    Adding column: $($Column.logicalName) (Uniqueidentifier → String(36))"
        return Invoke-DataverseApi -Method POST -Uri "$BaseUrl/EntityDefinitions(LogicalName='$EntityLogicalName')/Attributes" -Body $attrPayload -Headers $Headers
    }

    switch ($Column.type) {
        "String" {
            $attrPayload.MaxLength = if ($Column.maxLength) { $Column.maxLength } else { 200 }
            $attrPayload.FormatName = @{ Value = "Text" }
        }
        "Memo" {
            $attrPayload.MaxLength = if ($Column.maxLength) { $Column.maxLength } else { 1048576 }
            $attrPayload.Format = "TextArea"
        }
        "Integer" {
            $attrPayload.MinValue = -2147483648
            $attrPayload.MaxValue = 2147483647
            $attrPayload.Format = "None"
        }
        "BigInt" {
            $attrPayload.MinValue = -9223372036854775808
            $attrPayload.MaxValue = 9223372036854775807
        }
        "Decimal" {
            $precision = if ($Column.precision) { $Column.precision } else { 2 }
            $attrPayload.Precision = $precision
            $attrPayload.MinValue = -100000000000
            $attrPayload.MaxValue = 100000000000
        }
        "Boolean" {
            $attrPayload.OptionSet = @{
                TrueOption  = @{ Value = 1; Label = @{ "@odata.type" = "Microsoft.Dynamics.CRM.Label"; LocalizedLabels = @(@{ "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"; Label = "Yes"; LanguageCode = 1033 }) } }
                FalseOption = @{ Value = 0; Label = @{ "@odata.type" = "Microsoft.Dynamics.CRM.Label"; LocalizedLabels = @(@{ "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"; Label = "No"; LanguageCode = 1033 }) } }
            }
        }
        "DateTime" {
            $attrPayload.Format = "DateAndTime"
            $attrPayload.DateTimeBehavior = @{ Value = "UserLocal" }
        }
        "Picklist" {
            # Build options from schema if defined, otherwise create a single placeholder
            $options = @()
            if ($Column.options -and $Column.options.Count -gt 0) {
                foreach ($opt in $Column.options) {
                    $options += @{
                        Value = $opt.value
                        Label = @{ "@odata.type" = "Microsoft.Dynamics.CRM.Label"; LocalizedLabels = @(@{ "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"; Label = $opt.label; LanguageCode = 1033 }) }
                    }
                }
            }
            else {
                $options += @{
                    Value = 100000000
                    Label = @{ "@odata.type" = "Microsoft.Dynamics.CRM.Label"; LocalizedLabels = @(@{ "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"; Label = "Default"; LanguageCode = 1033 }) }
                }
            }
            $attrPayload.OptionSet = @{
                "@odata.type" = "Microsoft.Dynamics.CRM.OptionSetMetadata"
                IsGlobal = $false
                OptionSetType = "Picklist"
                Options = $options
            }
        }
        "Lookup" {
            $targetEntity = $Column.lookupTarget
            if (-not $targetEntity) {
                Write-Host "    WARNING: Lookup column $($Column.logicalName) has no lookupTarget — creating as Uniqueidentifier instead" -ForegroundColor Yellow
                $attrPayload."@odata.type" = "Microsoft.Dynamics.CRM.UniqueIdentifierAttributeMetadata"
                $attrPayload.Remove("RequiredLevel")
                break
            }

            # Lookups are created via relationship, not direct attribute creation
            $relationshipName = "$($EntityLogicalName)_$($Column.logicalName)_$targetEntity"
            $relationshipPayload = @{
                "@odata.type"        = "Microsoft.Dynamics.CRM.OneToManyRelationshipMetadata"
                SchemaName           = $relationshipName
                ReferencedEntity     = $targetEntity
                ReferencingEntity    = $EntityLogicalName
                Lookup               = @{
                    "@odata.type" = "Microsoft.Dynamics.CRM.LookupAttributeMetadata"
                    SchemaName    = $Column.logicalName
                    DisplayName   = @{
                        "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
                        LocalizedLabels = @(
                            @{
                                "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                                Label         = $Column.displayName
                                LanguageCode  = 1033
                            }
                        )
                    }
                }
            }

            Write-Host "    Creating lookup relationship: $($Column.logicalName) -> $targetEntity"
            return Invoke-DataverseApi -Method POST -Uri "$BaseUrl/RelationshipDefinitions" -Body $relationshipPayload -Headers $Headers
        }
    }

    Write-Host "    Adding column: $($Column.logicalName) ($($Column.type))"
    return Invoke-DataverseApi -Method POST -Uri "$BaseUrl/EntityDefinitions(LogicalName='$EntityLogicalName')/Attributes" -Body $attrPayload -Headers $Headers
}

# --- Provision entities ---

$createdEntities = @()
$skippedEntities = @()
$failedEntities = @()

foreach ($entity in $schema.entities) {
    $logicalName = $entity.logicalName
    $displayName = $entity.displayName
    $primaryId = $entity.primaryIdAttribute
    $primaryName = $entity.primaryNameAttribute

    Write-Host "--- [$logicalName] ---"

    # Check if entity already exists
    $check = Invoke-DataverseApi -Method GET -Uri "$baseUrl/EntityDefinitions(LogicalName='$logicalName')?`$select=LogicalName" -Headers $headers

    if ($check -and -not $check._error) {
        Write-Host "  Entity already exists — checking columns..."
        $skippedEntities += $logicalName

        # Get existing attributes
        $existingAttrs = Invoke-DataverseApi -Method GET -Uri "$baseUrl/EntityDefinitions(LogicalName='$logicalName')/Attributes?`$select=LogicalName" -Headers $headers
        $existingAttrNames = @()
        if ($existingAttrs.value) {
            $existingAttrNames = $existingAttrs.value | ForEach-Object { $_.LogicalName }
        }

        # Add missing columns
        $failedColumns = @()
        foreach ($col in $entity.columns) {
            if ($col.logicalName -in $existingAttrNames) {
                Write-Host "    Column $($col.logicalName) already exists — skipping"
                continue
            }
            Write-Host "    Adding missing column: $($col.logicalName)..."
            $attrResult = Add-ColumnToEntity -EntityLogicalName $logicalName -Column $col -BaseUrl $baseUrl -Headers $headers
            if ($attrResult -and $attrResult._error) {
                Write-Host "    WARNING: Failed to add column $($col.logicalName): $($attrResult.Message) — will retry" -ForegroundColor Yellow
                $failedColumns += $col
            }
        }

        # Retry failed columns once after a delay
        if ($failedColumns.Count -gt 0) {
            Write-Host "    Retrying $($failedColumns.Count) failed column(s) after 10s delay..."
            Start-Sleep -Seconds 10
            foreach ($col in $failedColumns) {
                $attrResult = Add-ColumnToEntity -EntityLogicalName $logicalName -Column $col -BaseUrl $baseUrl -Headers $headers
                if ($attrResult -and $attrResult._error) {
                    Write-Host "    FAILED (retry): $($col.logicalName): $($attrResult.Message)" -ForegroundColor Red
                }
                else {
                    Write-Host "    Retry succeeded: $($col.logicalName)" -ForegroundColor Green
                }
            }
        }
        continue
    }

    Write-Host "  Creating entity..."

    # Validate: primaryNameAttribute must not equal primaryIdAttribute
    if ($primaryName -eq $primaryId) {
        Write-Host "  FAILED: primaryNameAttribute '$primaryName' cannot be the same as primaryIdAttribute '$primaryId' — add a dedicated name column" -ForegroundColor Red
        $failedEntities += $logicalName
        continue
    }

    # Build primary name attribute definition
    $primaryNameCol = $entity.columns | Where-Object { $_.logicalName -eq $primaryName }
    $primaryNameMaxLength = 200
    if ($primaryNameCol -and $primaryNameCol.maxLength) {
        $primaryNameMaxLength = $primaryNameCol.maxLength
        if ($primaryNameMaxLength -gt 850) { $primaryNameMaxLength = 850 }
    }

    $entityPayload = @{
        SchemaName                = $logicalName
        "@odata.type"             = "Microsoft.Dynamics.CRM.EntityMetadata"
        DisplayName               = @{
            "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
            LocalizedLabels = @(
                @{
                    "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                    Label         = $displayName
                    LanguageCode  = 1033
                }
            )
        }
        DisplayCollectionName     = @{
            "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
            LocalizedLabels = @(
                @{
                    "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                    Label         = $entity.displayNamePlural
                    LanguageCode  = 1033
                }
            )
        }
        Description               = @{
            "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
            LocalizedLabels = @(
                @{
                    "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                    Label         = "RE2 Compliance Platform — $displayName"
                    LanguageCode  = 1033
                }
            )
        }
        HasActivities             = $false
        HasNotes                  = $false
        OwnershipType             = "OrganizationOwned"
        IsActivity                = $false
        PrimaryNameAttribute      = $primaryName
        Attributes                = @(
            @{
                "@odata.type"    = "Microsoft.Dynamics.CRM.StringAttributeMetadata"
                SchemaName       = $primaryName
                AttributeType    = "String"
                FormatName       = @{ Value = "Text" }
                MaxLength        = $primaryNameMaxLength
                RequiredLevel    = @{ Value = "ApplicationRequired" }
                DisplayName      = @{
                    "@odata.type"   = "Microsoft.Dynamics.CRM.Label"
                    LocalizedLabels = @(
                        @{
                            "@odata.type" = "Microsoft.Dynamics.CRM.LocalizedLabel"
                            Label         = if ($primaryNameCol) { $primaryNameCol.displayName } else { $primaryName }
                            LanguageCode  = 1033
                        }
                    )
                }
                IsPrimaryName = $true
            }
        )
    }

    $result = Invoke-DataverseApi -Method POST -Uri "$baseUrl/EntityDefinitions" -Body $entityPayload -Headers $headers

    if ($result -and $result._error) {
        Write-Host "  FAILED: $($result.Message)" -ForegroundColor Red
        $failedEntities += $logicalName
        continue
    }

    Write-Host "  Entity created successfully."
    $createdEntities += $logicalName

    # Wait for entity metadata to propagate before adding columns
    Write-Host "  Waiting 10s for entity metadata propagation..."
    Start-Sleep -Seconds 10

    # Add remaining columns (skip primary name — already created with entity)
    $failedColumns = @()
    foreach ($col in $entity.columns) {
        if ($col.logicalName -eq $primaryName) { continue }

        $attrResult = Add-ColumnToEntity -EntityLogicalName $logicalName -Column $col -BaseUrl $baseUrl -Headers $headers
        if ($attrResult -and $attrResult._error) {
            Write-Host "    WARNING: Failed to add $($col.logicalName): $($attrResult.Message) — will retry" -ForegroundColor Yellow
            $failedColumns += $col
        }
    }

    # Retry failed columns once after a delay
    if ($failedColumns.Count -gt 0) {
        Write-Host "  Retrying $($failedColumns.Count) failed column(s) after 10s delay..."
        Start-Sleep -Seconds 10
        foreach ($col in $failedColumns) {
            $attrResult = Add-ColumnToEntity -EntityLogicalName $logicalName -Column $col -BaseUrl $baseUrl -Headers $headers
            if ($attrResult -and $attrResult._error) {
                Write-Host "    FAILED (retry): $($col.logicalName): $($attrResult.Message)" -ForegroundColor Red
            }
            else {
                Write-Host "    Retry succeeded: $($col.logicalName)" -ForegroundColor Green
            }
        }
    }
}

# --- Create solution ---

Write-Host ""
Write-Host "=== Creating solution: $solutionName ==="

# Check for existing publisher
$publisherLogicalName = "${publisherPrefix}_publisher"
$publisherCheck = Invoke-DataverseApi -Method GET -Uri "$baseUrl/publishers?`$filter=customizationprefix eq '$publisherPrefix'&`$select=publisherid" -Headers $headers

$publisherId = $null
if ($publisherCheck.value -and $publisherCheck.value.Count -gt 0) {
    $publisherId = $publisherCheck.value[0].publisherid
    Write-Host "Publisher '$publisherPrefix' already exists: $publisherId"
}
else {
    Write-Host "Creating publisher '$publisherPrefix'..."
    $pubPayload = @{
        uniquename            = $publisherLogicalName
        friendlyname          = "RE2 Compliance Publisher"
        customizationprefix   = $publisherPrefix
        customizationoptionvalueprefix = 10000
    }
    $pubResult = Invoke-DataverseApi -Method POST -Uri "$baseUrl/publishers" -Body $pubPayload -Headers $headers
    if ($pubResult -and $pubResult._error) {
        Write-Host "WARNING: Failed to create publisher: $($pubResult.Message)" -ForegroundColor Yellow
    }
    else {
        # Re-fetch to get the ID
        $publisherCheck = Invoke-DataverseApi -Method GET -Uri "$baseUrl/publishers?`$filter=customizationprefix eq '$publisherPrefix'&`$select=publisherid" -Headers $headers
        if ($publisherCheck.value -and $publisherCheck.value.Count -gt 0) {
            $publisherId = $publisherCheck.value[0].publisherid
        }
    }
}

# Check for existing solution
$solutionCheck = Invoke-DataverseApi -Method GET -Uri "$baseUrl/solutions?`$filter=uniquename eq '$solutionName'&`$select=solutionid" -Headers $headers

if ($solutionCheck.value -and $solutionCheck.value.Count -gt 0) {
    Write-Host "Solution '$solutionName' already exists."
    $solutionId = $solutionCheck.value[0].solutionid
}
else {
    Write-Host "Creating solution '$solutionName'..."
    $solPayload = @{
        uniquename  = $solutionName
        friendlyname = $solutionDisplayName
        version     = $solutionVersion
        "publisherid@odata.bind" = "/publishers($publisherId)"
    }
    $solResult = Invoke-DataverseApi -Method POST -Uri "$baseUrl/solutions" -Body $solPayload -Headers $headers
    if ($solResult -and $solResult._error) {
        Write-Host "WARNING: Failed to create solution: $($solResult.Message)" -ForegroundColor Yellow
    }
    else {
        $solutionCheck = Invoke-DataverseApi -Method GET -Uri "$baseUrl/solutions?`$filter=uniquename eq '$solutionName'&`$select=solutionid" -Headers $headers
        if ($solutionCheck.value -and $solutionCheck.value.Count -gt 0) {
            $solutionId = $solutionCheck.value[0].solutionid
        }
    }
}

# Add entities to solution
Write-Host ""
Write-Host "=== Adding entities to solution ==="

foreach ($entity in $schema.entities) {
    $logicalName = $entity.logicalName
    Write-Host "  Adding $logicalName to solution..."

    $addPayload = @{
        ComponentId   = $null
        ComponentType = 1  # Entity
        SolutionUniqueName = $solutionName
        AddRequiredComponents = $false
    }

    # Get entity metadata ID
    $entityMeta = Invoke-DataverseApi -Method GET -Uri "$baseUrl/EntityDefinitions(LogicalName='$logicalName')?`$select=MetadataId" -Headers $headers
    if ($entityMeta -and -not $entityMeta._error) {
        $addPayload.ComponentId = $entityMeta.MetadataId

        $addResult = Invoke-DataverseApi -Method POST -Uri "$baseUrl/AddSolutionComponent" -Body $addPayload -Headers $headers
        if ($addResult -and $addResult._error) {
            Write-Host "    WARNING: $($addResult.Message)" -ForegroundColor Yellow
        }
        else {
            Write-Host "    Added."
        }
    }
    else {
        Write-Host "    WARNING: Could not find entity metadata for $logicalName" -ForegroundColor Yellow
    }
}

# --- Register Application Users (Managed Identities) ---

$registeredApps = @()
$failedApps = @()

if ($ResourceGroupName -and $AppNames) {
    Write-Host ""
    Write-Host "=== Registering Application Users (Managed Identities) ==="

    # Look up root business unit
    $buResult = Invoke-DataverseApi -Method GET -Uri "$baseUrl/businessunits?`$filter=parentbusinessunitid eq null&`$select=businessunitid,name" -Headers $headers
    if (-not $buResult.value -or $buResult.value.Count -eq 0) {
        Write-Host "WARNING: Could not find root business unit — skipping app user registration" -ForegroundColor Yellow
    }
    else {
        $rootBuId = $buResult.value[0].businessunitid
        Write-Host "Root business unit: $($buResult.value[0].name) ($rootBuId)"

        # Look up security role
        $roleResult = Invoke-DataverseApi -Method GET -Uri "$baseUrl/roles?`$filter=name eq '$DataverseRoleName' and _businessunitid_value eq '$rootBuId'&`$select=roleid,name" -Headers $headers
        $roleId = $null
        if ($roleResult.value -and $roleResult.value.Count -gt 0) {
            $roleId = $roleResult.value[0].roleid
            Write-Host "Security role: $DataverseRoleName ($roleId)"
        }
        else {
            Write-Host "WARNING: Security role '$DataverseRoleName' not found — users will be created without role assignment" -ForegroundColor Yellow
        }

        $appNameList = $AppNames -split ","
        foreach ($appName in $appNameList) {
            $appName = $appName.Trim()
            if (-not $appName) { continue }

            Write-Host ""
            Write-Host "--- [$appName] ---"

            # Get managed identity principal ID (works for App Services and Function Apps)
            $principalId = $null
            try {
                $principalId = az resource show --resource-group $ResourceGroupName --name $appName --resource-type "Microsoft.Web/sites" --query identity.principalId -o tsv 2>&1
                if ($LASTEXITCODE -ne 0) { $principalId = $null }
            }
            catch { $principalId = $null }

            if (-not $principalId) {
                Write-Host "  WARNING: Could not find managed identity for $appName — skipping" -ForegroundColor Yellow
                $failedApps += $appName
                continue
            }
            Write-Host "  Principal ID (Object ID): $principalId"

            # Look up Application (Client) ID from Azure AD service principal
            # Requires Directory.Read.All or Application.Read.All on the pipeline service connection
            $appId = $null
            try {
                $appId = az ad sp show --id $principalId --query appId -o tsv 2>&1
                if ($LASTEXITCODE -ne 0) { $appId = $null }
            }
            catch { $appId = $null }

            if (-not $appId) {
                Write-Host "  WARNING: Could not resolve Application (Client) ID for $appName." -ForegroundColor Yellow
                Write-Host "  Ensure the pipeline service connection has Directory.Read.All permission in Azure AD." -ForegroundColor Yellow
                $failedApps += $appName
                continue
            }
            Write-Host "  Application (Client) ID: $appId"

            # Check if application user already exists in Dataverse
            $existingUser = Invoke-DataverseApi -Method GET -Uri "$baseUrl/systemusers?`$filter=applicationid eq '$appId'&`$select=systemuserid,fullname" -Headers $headers
            if ($existingUser.value -and $existingUser.value.Count -gt 0) {
                $systemUserId = $existingUser.value[0].systemuserid
                Write-Host "  Application user already exists: $($existingUser.value[0].fullname) ($systemUserId)"
            }
            else {
                # Create application user
                Write-Host "  Creating application user..."
                $userPayload = @{
                    applicationid                = $appId
                    firstname                    = "RE2"
                    lastname                     = $appName
                    internalemailaddress         = "$appName@re2.local"
                    domainname                   = "$appName@re2.local"
                    accessmode                   = 4  # Non-interactive
                    "businessunitid@odata.bind"  = "/businessunits($rootBuId)"
                    isdisabled                   = $false
                    islicensed                   = $false
                }
                $createResult = Invoke-DataverseApi -Method POST -Uri "$baseUrl/systemusers" -Body $userPayload -Headers $headers
                if ($createResult -and $createResult._error) {
                    Write-Host "  FAILED to create application user: $($createResult.Message)" -ForegroundColor Red
                    $failedApps += $appName
                    continue
                }

                # Re-query to get the systemuserid
                Start-Sleep -Seconds 3
                $existingUser = Invoke-DataverseApi -Method GET -Uri "$baseUrl/systemusers?`$filter=applicationid eq '$appId'&`$select=systemuserid,fullname" -Headers $headers
                if ($existingUser.value -and $existingUser.value.Count -gt 0) {
                    $systemUserId = $existingUser.value[0].systemuserid
                    Write-Host "  Created: $($existingUser.value[0].fullname) ($systemUserId)" -ForegroundColor Green
                }
                else {
                    Write-Host "  FAILED: User created but could not be retrieved" -ForegroundColor Red
                    $failedApps += $appName
                    continue
                }
            }

            # Assign security role (idempotent)
            if ($roleId) {
                Write-Host "  Ensuring role '$DataverseRoleName' is assigned..."
                $rolePayload = @{
                    "@odata.id" = "$baseUrl/roles($roleId)"
                }
                $roleAssignResult = Invoke-DataverseApi -Method POST -Uri "$baseUrl/systemusers($systemUserId)/systemuserroles_association/`$ref" -Body $rolePayload -Headers $headers
                if ($roleAssignResult -and $roleAssignResult._error) {
                    if ($roleAssignResult.StatusCode -eq 409 -or ($roleAssignResult.Message -and $roleAssignResult.Message -match "Cannot insert duplicate key")) {
                        Write-Host "  Role already assigned."
                    }
                    else {
                        Write-Host "  WARNING: Failed to assign role: $($roleAssignResult.Message)" -ForegroundColor Yellow
                    }
                }
                else {
                    Write-Host "  Role assigned." -ForegroundColor Green
                }
            }

            $registeredApps += $appName
        }
    }
}

# --- Summary ---

Write-Host ""
Write-Host "=== Provisioning Summary ==="
Write-Host "  Created: $($createdEntities.Count) entities"
if ($createdEntities.Count -gt 0) {
    $createdEntities | ForEach-Object { Write-Host "    - $_" }
}
Write-Host "  Existing (checked columns): $($skippedEntities.Count) entities"
if ($skippedEntities.Count -gt 0) {
    $skippedEntities | ForEach-Object { Write-Host "    - $_" }
}
Write-Host "  Failed: $($failedEntities.Count) entities"
if ($failedEntities.Count -gt 0) {
    $failedEntities | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
}
if ($registeredApps.Count -gt 0) {
    Write-Host "  Application users registered: $($registeredApps.Count)"
    $registeredApps | ForEach-Object { Write-Host "    - $_" }
}
if ($failedApps.Count -gt 0) {
    Write-Host "  Application users failed: $($failedApps.Count)"
    $failedApps | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow }
}
Write-Host ""

if ($failedEntities.Count -gt 0) {
    Write-Host "PROVISIONING COMPLETED WITH ERRORS" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "PROVISIONING COMPLETED SUCCESSFULLY" -ForegroundColor Green
    exit 0
}
