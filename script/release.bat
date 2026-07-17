echo ================================
echo =       BUILD RELEASE          =
echo ================================

< NUL call config.bat

rem echo %REPO_RELEASE_PATH%

rmdir /S /Q %REPO_RELEASE_PATH%

rem echo %REPO_RELEASE_MOD_PATH%

mkdir %REPO_RELEASE_PATH%
mkdir %REPO_RELEASE_MOD_PATH%

copy %REPO_PATH%VSCODE\meta.json %REPO_RELEASE_MOD_PATH%
copy %REPO_BUILD_PATH%%DLL_MOD_FILE_NAME% %REPO_RELEASE_MOD_PATH%
copy %REPO_BUILD_PATH%%PDB_MOD_FILE_NAME% %REPO_RELEASE_MOD_PATH%
xcopy /S /E /Y %REPO_PATH%ModFile %REPO_RELEASE_MOD_PATH%