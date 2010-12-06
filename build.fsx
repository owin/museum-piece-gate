#I "tools/FAKE"
#r "FakeLib.dll"
open Fake 
open Fake.MSBuild

(* properties *)
let projectName = "owin"
let version = "1.0.0.0"  

(* Directories *)
let buildDir = "./build/"
let docsDir = "./docs/" 
let deployDir = "./deploy/"
let testDir = "./test/"

(* Tools *)
let nunitPath = "./tools/Nunit"
let nunitOutput = testDir + "TestResults.xml"
let zipFileName = deployDir + sprintf "%s-%s.zip" projectName version

(* files *)
let appReferences =
    !+ @"src\**\*.csproj" 
      ++ @"src\**\*.fsproj"
      -- "**\*_Spliced*" 
        |> Scan

let filesToZip =
  !+ (buildDir + "/**/*.*")
      -- "*.zip"
      |> Scan      

(* Targets *)
Target? Clean <-
    fun _ -> CleanDirs [buildDir; testDir; deployDir; docsDir]

Target? BuildApp <-
    fun _ -> 
        if not isLocalBuild then
          AssemblyInfo
           (fun p -> 
              {p with
                 CodeLanguage = CSharp;
                 AssemblyVersion = buildVersion;
                 AssemblyTitle = "Open Web Interface for .NET";
                 AssemblyDescription = "A standard set of interfaces for developing common runtimes and frameworks for .NET web development.";
                 Guid = "e59a3128-208a-4999-821a-4dfed4a1c23c";
                 OutputFileName = "./src/Properties/AssemblyInfo.cs"})

        appReferences
          |> Seq.map (RemoveTestsFromProject AllNUnitReferences AllSpecAndTestDataFiles)
          |> MSBuildRelease buildDir "Build"
          |> Log "AppBuild-Output: "

Target? BuildTest <-
    fun _ -> 
        appReferences
          |> MSBuildDebug testDir "Build"
          |> Log "TestBuild-Output: "

Target? Test <-
    fun _ ->
        !+ (testDir + "/*.dll")
          |> Scan
          |> NUnit (fun p -> 
                      {p with 
                         ToolPath = nunitPath; 
                         DisableShadowCopy = true; 
                         OutputFile = nunitOutput}) 

Target? GenerateDocumentation <-
    fun _ ->
      !+ (buildDir + "owin.dll")      
        |> Scan
        |> Docu (fun p ->
            {p with
               ToolPath = "./tools/FAKE/docu.exe"
               TemplatesPath = "./tools/FAKE/templates"
               OutputPath = docsDir })

Target? BuildZip <-
    fun _ -> Zip buildDir zipFileName filesToZip

Target? ZipDocumentation <-
    fun _ ->    
        let docFiles = 
          !+ (docsDir + "/**/*.*")
            |> Scan
        let zipFileName = deployDir + sprintf "Documentation-%s.zip" version
        Zip docsDir zipFileName docFiles

Target? Default <- DoNothing
Target? Deploy <- DoNothing

// Dependencies
For? BuildApp <- Dependency? Clean
For? Test <- Dependency? BuildApp |> And? BuildTest
For? GenerateDocumentation <- Dependency? BuildApp
For? ZipDocumentation <- Dependency? GenerateDocumentation
For? BuildZip <- Dependency? Test
For? Deploy <- Dependency? ZipDocumentation |> And? BuildZip
For? Default <- Dependency? Deploy

// start build
Run? Default
