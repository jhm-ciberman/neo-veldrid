# Native AOT

All NeoVeldrid packages are compatible with [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) and trimming. You can publish your application as a single native executable:

```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true
```

The native libraries NeoVeldrid depends on, such as SDL2, shaderc, and SPIRV-Cross, are copied next to the executable.
