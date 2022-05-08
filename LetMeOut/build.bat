dotnet build . -c Release
del /F /Q mod.zip
7z a -tzip mod.zip README.md manifest.json icon.png artifactEnabled.png artifactDisabled.png .\bin\Release\netstandard2.0\LetMeOut.dll