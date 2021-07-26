<#
.Synopsis Remove last migration for multiple database providers
.Notes
    From the project root, pass in the context.
    If you just have a single context, assign it to the parameter
.Example
    ./Data/migrations-undo.ps1 ClientDb
#>
Param(
    # [Parameter(Mandatory = $true)]
    # $context
)
$context='GameboardDb'
$providers = @('SqlServer', 'PostgreSQL')
foreach ($provider in $providers) {
    $env:Database:Provider=$provider
    dotnet ef migrations remove --context "$($context)Context$($provider)" -f
}
$env:Database:Provider=''
