## MSBuildTasks

This is a library containing custom MSBuild tasks that I use in the process of speeding up project build times 
   
### Examples:

CUstom tasks defined within can be used in the following manner:

```
<Project Sdk="Microsoft.NET.Sdk.Razor">

  ...
  
  <UsingTask TaskName="CopyToReferencingProjectsMSBuildTask" AssemblyFile="G:\Moje Pliki\Programowanie\CSharp\Projects\MSBuildTasks\MSBuildTasks\MSBuildTasks\bin\Debug\netstandard2.0\MSBuildTasks.dll" />
  <Target Name="CopyContentAfterBuild" BeforeTargets="Rebuild">
    <Message Importance="high" Text="PublishUrl: $(PublishDir)"></Message> 
    <Message Importance="high" Text="OutDir: $(OutDir)"></Message> 
    <CopyToReferencingProjectsMSBuildTask SolutionDir="$(SolutionDir)" ProjectDir="$(ProjectDir)" OutDirPart="$(OutDir)" SourceFilePatterns="Content\**\*.*" IncludePublish="true" CheckFiles="false" />
  </Target>

</Project>
```


