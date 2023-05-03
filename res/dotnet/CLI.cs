using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Orkestra;

public abstract class CLI
{
    public void Run(params string[] args)
    {
        if (args.Length > 0)
            call(args[0], args[1..]);
        else help();
    }

    protected void call(string command, string[] otherArgs)
    {
        var cliType = this.GetType();
        foreach (var method in cliType.GetRuntimeMethods())
        {
            if (method.Name == command)
            {
                var parameters = new List<object>();
                foreach (var parameter in method.GetParameters())
                {
                    //TODO
                }
                method.Invoke(this, parameters.ToArray());
                return;
            }
        }

        help();
    }

    protected void help()
    {

    }
}