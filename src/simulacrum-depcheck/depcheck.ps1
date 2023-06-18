function Assert-Dependency {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string] $Executable
    )

    if ($null -eq (Get-Command $Executable -ErrorAction SilentlyContinue)) {
        Write-Output "$Executable is not installed, please install it!"
        exit 1
    }
}

Assert-Dependency "yarn"
Assert-Dependency "docker"
Assert-Dependency "python3"

Write-Output ""
Write-Output "All environment dependencies are installed!"
