"%WIX%bin\heat.exe" dir "D:\dev\file-share-service-desktop-client\file-share-service-desktop-client\bin\Release\net5.0-windows\win-x64\publish" -cg ApplicationFilesComponentGroup -dr INSTALLFOLDER -gg -sfrag -scom -sreg -srd -var var.FileShareService.DesktopClient.TargetDir -out "ApplicationFilesComponents.wxs" -platform "x64"