#I "./packages/FAKE.1.62.1/tools"
#r "FakeLib.dll"

open Fake 


// properties
let projectName = "Gate"
let version = "0.2.1"  
let projectSummary = "An OWIN utility library."
let projectDescription = "An OWIN utility library."
let authors = ["bvanderveen";"grumpydev";"jasonsirota";"loudej";"markrendle";"thecodejunkie";"panesofglass"]
let mail = "b@bvanderveen.com"
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
Target "Clean" (fun _ ->
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

Target "Test" (fun _ ->
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

Target "Deploy" (fun _ ->
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

// Build order
"Clean"
  ==> "Version"
  ==> "BuildApp" <=> "BuildTest"
  ==> "Test" //<=> "GenerateDocumentation"
  //==> "ZipDocumentation"
  ==> "Deploy"

// Start build
Run "Deploy"


