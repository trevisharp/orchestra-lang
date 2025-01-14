/* Author:  Leonardo Trevisan Silio
 * Date:    01/07/2023
 */
using System;

namespace Orkestra.Processings;

/// <summary>
/// A processing that can be defined from a Func<Text, Text>
/// </summary>
public class FuncProcessing(Func<Text, Text> func) : Processing {
    public override Text Process(Text text) => func(text);
}