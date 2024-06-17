/* Author:  Leonardo Trevisan Silio
 * Date:    12/06/2023
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orkestra.Extensions.VSCode;

/// <summary>
/// Represents a VSCode Extension.
/// </summary>
public class VSCodeExtension : Extension
{
    List<VSCodeContribute> contributes = new();
    public IEnumerable<VSCodeContribute> Contributes => contributes;
    public void Add(VSCodeContribute contribute)
    {
        if (contribute is null)
            return;
        
        this.contributes.Add(contribute);
    }

    public override async Task Generate(ExtensionArguments args)
    {
        var path = createTempFolder();
        await addPackageJson(path, args);
        await addReadme(path, args);
        await addContributes(path, args);
        await addExtensionJS(path, args);
        await addChangeLog(path, args);
        await zip(path, args.Name + ".vsix");
        await install(path, args.Name + ".vsix");
    }

    async Task addPackageJson(string dir, ExtensionArguments args)
    {
        const string file = "package.json";
        var sw = open(dir, file);

        var name = args.Name;

        await sw.WriteAsync(
            $$"""
            {
                "name": "{{name}}"
                "displayName": "{{name}}",
                "description": "A autogenerated Orkestra VSCode extension of {{name}}.",
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
            """
        );

        var contributeGroups = 
            from c in contributes
            group c by c.Type;

        foreach (var g in contributeGroups)
        {
            var sb = new StringBuilder();
            var contName = g.Key.ToCamelCase();
            sb.AppendLine($"\t\t\"{contName}\": [");

            foreach (var contribute in g)
                sb.AppendLine(contribute.Declaration);

            sb.AppendLine("],");
            await sw.WriteAsync(sb.ToString());
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
            await contribute?.GenerateFile(dir, args);
    }

    async Task addReadme(string dir, ExtensionArguments args)
    {

    }

    async Task addExtensionJS(string dir, ExtensionArguments args)
    {
        const string file = "extension.js";
        var sw = open(dir, file);

        await sw.WriteAsync(
            $$"""
            const vscode = require('vscode');

            function activate(context) {}

            function deactivate() {}

            module.exports = {
                activate,
                deactivate
            }
            """
        );
    }

    async Task addChangeLog(string dir, ExtensionArguments args)
    {
 
    }

    async Task zip(string dir, string output)
    {

    }

    async Task install(string dir, string file)
    {

    }

    StreamWriter open(string folder, string file)
    {
        var path = Path.Combine(folder, file);
        var sw = new StreamWriter(path);
        return sw;
    }

    string createTempFolder()
    {
        var path = getRandomTempFile();
        initFolder(path);
        return path;
    }

    string getRandomTempFile()
    {
        var temp = Path.GetTempFileName();
        var guid = Guid.NewGuid().ToString();
        var path = Path.Combine(temp, guid);
        return path;
    }

    void initFolder(string path)
    {
        deleteIfExists(path);
        createFile(path);
    }

    void deleteIfExists(string path)
    {
        if (!Path.Exists(path))
            return;

        var dir = new DirectoryInfo(path);
        dir.Delete(true);
    }

    void createFile(string path)
        => Directory.CreateDirectory(path);
}