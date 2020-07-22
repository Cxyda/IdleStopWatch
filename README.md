# IdleStopWatch
An idle time stopwatch for Unity3D

This tiny project measures and accumulates the time you have to wait on Unity3D.

It measures time Unity3D needs to compile scripts and reloading of the AssetDatabase and loggs them to the console. 
It also accumulates the overall waiting time of the project and stores it in the `EditorPrefs`

## How To Use It

You only have to put the `IdleTimeCounter.cs` into an `Editor` folder somewhere in your project. The rest works out of the box.
