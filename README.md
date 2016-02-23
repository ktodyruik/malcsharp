malcsharp
=========

This is a modified copy of the CSharp implementation of [Make-A-Lisp](https://github.com/kanaka/mal).

Goal
----

To make a practical CSharp project REPL, interfacing with existing CSharp code.

Modifications to the original MAL code
--------------------------------------

The MAL directory contains the CSharp implementation copied exactly (for easy upgrade) with a couple of small modifications to 'stepA_MAL.cs':

-	"Main" method name changed to "MainOriginal"
-	public method 'RE' added
-	public method 'PRINT' added

The "Interpreter" class contains most enhancements including some added interop methods.

Uses Microsoft.Dynamic.Scripting for the readline functionality instead of the readline code that was included in MAL.

Also, outputs to the console in color.

Usage
-----

Run the console app, starting the MAL REPL.

Note that 'prelude.mal' script will be run on startup. Modify this to run scripts on starting the shell.

Press ctrl-c or (exit) to exit the repl.

Examples
--------

```
; Call a custom function (defined in Interpreter.cs)
> (ping)
"Pong"

; Reference an assembly dynamically (not necessary if your assembly is already referenced in your project)
> (clr-reference "AssemblyName")
nil

; Include the test class and make some static calls
; Actually this is already in prelude.mal
> (clr-using "Shell.Testing.Test")
nil

> (clr-static-call "Test" "Random")
15

; Run the now function defined in the prelude file
> (now)
"February 26, 2016"

; Run a function with a parameter
> (clr-static-call "Test" "RandomNumbers" 3)
(72 2 91)

; Load pprint.mal (not really necessary as it is loaded in prelude.mal)
> (load-file "pp.mal")

; Try out pprint
> (pprint '(1 2 3))

; Load and call test functions
> (load-file "test.mal")
nil

> (square 3)
9

; Display an object
> (clr-static-call "Test" "Person")
{"Name" "Kerry" "Age" "43" "Numbers" (1 2 3 4 5) "Person" {"Name" "Long John Silver" "Age" nil "Numbers" nil "Person" nil}}

;; PPrint an object with a nested object
> (def! person (clr-static-call "Test" "Person"))
> (pprint person)
{"Name" "Kerry"
 "Age" "43"
 "Numbers" (1
       2
       3
       4
       5)
 "Person" {"Name" "Long John Silver"
       "Age" nil
       "Numbers" nil
       "Person" nil}}
nil

;; Use keywords and use the eq? alias instead of =. eq? is in prelude.mal

> (def! kw :abc)
:abc

> (= kw :abc)
true

> (eq? kw :abc)
true



```
