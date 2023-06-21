﻿using System.Linq;

string code = 
@"
##########################################################################################################
#######################################  Pre-processing definition  ######################################

processing all:
	int level = 0
	int current = 0
	bool emptyline = true

	processing line:
		emptyline = true
		current = 0

		processing character:
			if character is ""\35"":
				discard
			if character not is tab and character not is newline and character not is space:
				emptyline = false
		
		if emptyline:
			skip
		
		processing character:
            if character is tab:
                current += 4
            else if character is space:
                current += 1
            else:
                break

    	if current > level + 4:
			throw TabulationError

		if current > level:
			level = current
			prepend newline STARTBLOCK
		
		append ENDLINE

		while level > current:
			level -= 4
			prepend newline ENDBLOCK
    while level > current:
        level -= 4
        prepend newline ENDBLOCK
	append ENDFILE
";

SymphonyCompiler compiler = new SymphonyCompiler();
compiler.Verbose = args.Contains("-v") || args.Contains("--verbose");

compiler.Compile(code);