#I "./packages/FAKE.1.52.6.0/tools"
#r "FakeLib.dll"

open Fake 

// properties
let projectName = "Gate"
let version = "0.1.0"  
let projectSummary = "An OWIN utility library."
let projectDescription = "An OWIN utility library."
let authors = ["bvanderveen";"grumpydev";"jasonsirota";"loudej";"markrendle";"thecodejunkie";"panesofglass"]
let mail = "b@bvanderveen.com"
let homepage = "http://github.com/owin/gate"

// directories
let buildDir = "./build/"
let testDir = "./test/"
let deployDir = "./deploy/"
let docsDir = "./docs/"

// tools
let fakePath = "./packages/FAKE.1.52.6.0/tools"
let nunitPath = "./packages/NUnit.2.5.9.10348/Tools"

// files
let appReferences =
    !+ "./src/Gate/**/*.*sproj"
      ++ "./src/Gate.Helpers/**/*.*sproj"
      ++ "./src/Hosts/**/*.*sproj"
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
    CleanDirs [buildDir; testDir; deployDir]
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    printfn "%A" testReferences
    MSBuildDebug testDir "Build" testReferences
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
    !+ (buildDir + "/**/*.*")
        -- "*.zip"
        |> Scan
        |> Zip buildDir (deployDir + sprintf "%s-%s.zip" projectName version)
)

// Build order
"Clean"
  ==> "BuildApp" <=> "BuildTest"
  ==> "Test" <=> "GenerateDocumentation"
  ==> "ZipDocumentation"
  ==> "Deploy"

// Start build
Run "Deploy"
