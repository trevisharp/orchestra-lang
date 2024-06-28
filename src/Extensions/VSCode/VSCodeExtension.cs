/* Author:  Leonardo Trevisan Silio
 * Date:    28/06/2023
 */
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Orkestra.Extensions.VSCode;

/// <summary>
/// Represents a VSCode Extension.
/// </summary>
public class VSCodeExtension : Extension
{
    List<VSCodeContribute> contributes = new();
    List<JSContribute> jscontributes = new();
    public IEnumerable<VSCodeContribute> Contributes => contributes;
    public IEnumerable<JSContribute> JSContributes => jscontributes;
    
    public void Add(VSCodeContribute contribute)
    {
        if (contribute is null)
            return;
        
        this.contributes.Add(contribute);
    }

    public void Add(JSContribute contribute)
    {
        if (contribute is null)
            return;
        
        this.jscontributes.Add(contribute);
    }

    public virtual void LoadDefaultContributes(ExtensionArguments args)
    {
        foreach (var lang in args.Languages)
        {
            Add(new LanguageContribute(lang));
            Add(new GrammarContribute(lang));
            Add(new AutoCompleteJSContribute(lang));
        }
    }

    public override async Task Generate(ExtensionArguments args)
    {
        Verbose.Info("Generating VSCode Extension...", 1);
        Verbose.NewLine();

        LoadDefaultContributes(args);

        var path = createTempFolder();
        Verbose.Info($"Temp directory created on {path}.", 3);

        var extPath = initExtensionDirectory(path);

        await addPackageJson(extPath, args);
        await addReadme(extPath, args);
        await addContributes(extPath, args);
        await addExtensionJS(extPath, args);
        await addChangeLog(extPath, args);
        
        var finalFile = args.Name + ".vsix";
        deleteFileIfExists(finalFile);
        zip(path, finalFile);

        deleteFolderIfExists(path);
        Verbose.Info($"Temp directory removed from {path}.", 3);
    }
    
    public override async Task Install(ExtensionArguments args)
    {
        var finalFile = args.Name + ".vsix";
        await Generate(args);
        install(finalFile);
        deleteFileIfExists(finalFile);
    }

    string initExtensionDirectory(string dir)
    {
        var extensionPath = Path.Combine(dir, "extension");

        Directory.CreateDirectory(extensionPath);
        return extensionPath;
    }

    async Task addPackageJson(string dir, ExtensionArguments args)
    {
        const string file = "package.json";
        var sw = open(dir, file);

        var name = args.Name;

        await sw.WriteLineAsync(
            $$"""
            {
                "name": "{{name}}",
                "displayName": "{{name}}",
                "publisher": "Orkestra",
                "description": "A autogenerated Orkestra VSCode extension for {{name}}.",
                "version": "1.0.0",
                "engines": {
                    "vscode": "^1.89.0"
                },
                "categories": [
                    "Programming Languages"
                ],
                "activationEvents": [],
                "main": "./extension.js",
                "contributes": {
                    "commands": [{
                        "command": "bruteforce.helloWorld",
                        "title": "Hello World"
                    }],
            """
        );

        var contributeGroups = 
            from c in contributes
            group c by c.Type;
        var contributeGroupsArray = 
            contributeGroups.ToArray();

        for (int i = 0; i < contributeGroupsArray.Length; i++)
        {
            var g = contributeGroupsArray[i];
            var contName = g.Key.ToString().ToLower();
            await sw.WriteLineAsync($"\t\t\"{contName}\": [");

            foreach (var contribute in g)
                await sw.WriteLineAsync("\t\t" + 
                    contribute.Declaration
                        .Replace("\n\r", "\t\t")
                        .Replace("\n", "\t\t")
                );

            if (i < contributeGroupsArray.Length - 1)
                await sw.WriteLineAsync("\t],");
            else await sw.WriteLineAsync("\t]");
        }

        await sw.WriteAsync(
            """
                },                
                "scripts": {
                    "lint": "eslint .",
                    "pretest": "npm run lint",
                    "test": "vscode-test"
                },
                "devDependencies": {
                    "@types/vscode": "^1.89.0",
                    "@types/mocha": "^10.0.6",
                    "@types/node": "18.x",
                    "eslint": "^8.57.0",
                    "typescript": "^5.4.5",
                    "@vscode/test-cli": "^0.0.9",
                    "@vscode/test-electron": "^2.4.0"
                }
            }
            """
        );

        sw.Close();
    }

    async Task addContributes(string dir, ExtensionArguments args)
    {
        foreach (var contribute in this.contributes)
            await contribute?.GenerateFile(dir);
    }

    async Task addReadme(string dir, ExtensionArguments args)
    {
        const string file = "README.md";
        var sw = open(dir, file);

        await sw.WriteLineAsync(
            $"""
            # {args.Name}
            
            A autogenerated VSCode extension of {args.Name} using Orkestra Framework.
            
            ## Features
            """
        );

        var contributeGroups = 
            from c in contributes
            group c by c.Type;

        foreach (var g in contributeGroups)
        {
            await sw.WriteLineAsync($"### {g.Key}\n");
            foreach (var cont in g)
                await sw.WriteLineAsync(cont.Documentation);
            await sw.WriteLineAsync();
        }

        sw.Close();
    }

    async Task addExtensionJS(string dir, ExtensionArguments args)
    {
        const string file = "extension.js";
        var sw = open(dir, file);

        var js = new StringBuilder();

        foreach (var jscontribute in jscontributes)
        {
            js.AppendLine(
                jscontribute.JSCode.Replace("\n", "\n\t")
            );
        }

        await sw.WriteAsync(
            $$"""
            const vscode = require('vscode');

            function activate(context) {
            {{js}}
            }

            function deactivate() {}

            module.exports = {
                activate,
                deactivate
            }
            """
        );

        sw.Close();
    }

    async Task addChangeLog(string dir, ExtensionArguments args)
    {
        const string file = "CHANGELOG.md";
        var sw = open(dir, file);

        await sw.WriteLineAsync("## Version 1.0.0");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync("All elements auto generated.");

        sw.Close();
    }

    void zip(string dir, string file)
        => ZipFile.CreateFromDirectory(dir, file);

    void install(string output)
    {
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            Arguments = $"/C code --install-extension {output}"
        };
        var process = new Process()
        {
            StartInfo = startInfo
        };

        Verbose.NewLine();
        Verbose.Info("Visual Studio Code Output:");
        Verbose.NewLine();
        process.Start();
        process.WaitForExit();
    }

    StreamWriter open(string folder, string file)
    {
        var path = Path.Combine(folder, file);
        var sw = new StreamWriter(path);
        return sw;
    }

    string createTempFolder()
    {
        var path = getRandomTempFolder();
        while (Directory.Exists(path))
            path = getRandomTempFolder();

        initFolder(path);
        return path;
    }

    string getRandomTempFolder()
    {
        var temp = Path.GetTempPath();
        var guid = Guid.NewGuid().ToString();
        var path = Path.Combine(temp, guid);
        return path;
    }

    void initFolder(string path)
    {
        deleteFolderIfExists(path);
        Directory.CreateDirectory(path);
    }

    void deleteFileIfExists(string path)
    {
        if (!File.Exists(path))
            return;
        
        File.Delete(path);
    }

    void deleteFolderIfExists(string path)
    {
        if (!Path.Exists(path))
            return;

        var dir = new DirectoryInfo(path);
        dir.Delete(true);
    }
}