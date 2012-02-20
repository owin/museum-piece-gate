#I "./packages/FAKE.1.62.1/tools"
#r "FakeLib.dll"

open Fake 


// properties
let projectName = "Gate"
let version = "0.3.4"  
let projectSummary = "An OWIN utility library."
let projectDescription = "An OWIN utility library."
let authors = ["bvanderveen";"grumpydev";"jasonsirota";"loudej";"markrendle";"thecodejunkie";"panesofglass"]
let mail = "net-http-abstractions@googlegroups.com"
let homepage = "http://github.com/owin/gate"

// directories
let targetDir = "./target/"
let buildDir = targetDir + "build/"
let testDir = targetDir + "test/"
let deployDir = targetDir + "deploy/"
let docsDir = targetDir + "docs/"

// tools
let fakePath = "./packages/FAKE.1.52.6.0/tools"
let nunitPath = "./packages/NUnit.2.5.10.11092/Tools"
let nugetPath = "./.nuget/nuget.exe"

// files
let appReferences =
    !+ "./src/Main/**/*.*sproj"
      ++ "./src/Hosts/**/*.*sproj"
      ++ "./src/Deploy/Deploy.csproj"
      |> Scan

let testReferences =
    !+ "./src/Tests/**/*.*sproj"
      |> Scan

let filesToZip =
    !+ (buildDir + "/**/*.*")
      -- "*.zip"
      |> Scan

// targets
Target "CleanTargetDir" (fun _ ->
    CleanDirs [targetDir]
)

let ApplyVersion files =
  for file in files do
    ReplaceAssemblyInfoVersions (fun p ->
          {p with 
            AssemblyVersion = version;
            AssemblyFileVersion = version;
            OutputFileName = file; })

Target "Version" (fun _ ->
    !+ ("./src/**/AssemblyInfo.cs")
        |> Scan
        |> ApplyVersion
)

Target "BuildApp" (fun _ ->
    MSBuild buildDir "Build" ["Configuration","Release"; "PackageVersion",version] appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    printfn "%A" testReferences
    MSBuild testDir "Build"  ["Configuration","Debug"] testReferences
        |> Log "TestBuild-Output: "
)

Target "NUnitTest" (fun _ ->
    !+ (testDir + "/*.Tests.dll")
        |> Scan
        |> NUnit (fun p ->
            {p with
                ToolPath = nunitPath
                DisableShadowCopy = true
                OutputFile = testDir + "TestResults.xml" })
)

Target "GenerateDocumentation" (fun _ ->
    !+ (buildDir + "*.dll")
        |> Scan
        |> Docu (fun p ->
            {p with
                ToolPath = fakePath + "/docu.exe"
                TemplatesPath = fakePath + "/templates"
                OutputPath = docsDir })
)

Target "ZipDocumentation" (fun _ ->
    !+ (docsDir + "/**/*.*")
        |> Scan
        |> Zip docsDir (deployDir + sprintf "Documentation-%s.zip" version)
)

Target "PackageZip" (fun _ ->
    CreateDir deployDir
    !+ (buildDir + "/**/*.nupkg")
        |> Scan
        |> Copy deployDir

    !+ (buildDir + "/**/Gate*.dll")
        ++ (buildDir + "/**/Gate*.pdb")
        -- "*.zip"
        |> Scan
        |> Zip buildDir (deployDir + sprintf "%s-%s.zip" projectName version)
)

Target "InstallPackages" (fun _ ->
  let userLocalRepository = (environVar "HOME") @@ ".nuget";

  !! (buildDir @@ "**/*.nupkg") |> Copy userLocalRepository
)


Target "UploadPackages" (fun _ ->
  let apply files =
    for file in files do
      ExecProcess (fun info ->
                info.FileName <- nugetPath
                info.WorkingDirectory <- deployDir |> FullName
                info.Arguments <-  sprintf "push \"%s\"" (file |> FullName)) (System.TimeSpan.FromMinutes 5.)

  !! (deployDir @@ "*.nupkg") |> apply
)


let Phase name = (
  Target name (fun _ -> trace "----------")
  name
)

// build phases
Phase "Clean"
  ==> Phase "Initialize" 
  ==> Phase "Process" 
  ==> Phase "Compile" 
  ==> Phase "Test" 
  ==> Phase "Package"
  ==> Phase "Install"
  ==> Phase "Deploy" 

Phase "Default" <== ["Package"]

// build phase goals
"Clean" <== ["CleanTargetDir"]
"Process" <== ["Version"]
"Compile" <== ["BuildApp"; "BuildTest"]
"Test" <== ["NUnitTest"]
"Package" <== ["PackageZip"]
"Install" <== ["InstallPackages"]
"Deploy" <== ["UploadPackages"]

// start build
Run <| getBuildParamOrDefault "target" "Default"


