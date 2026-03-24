' NULLFRAME — Silent GUI Launcher (no CMD window)
' Double-click this file to open the app without any console window.

Option Explicit

Dim oFSO, oShell, oSA, sDir

Set oFSO   = CreateObject("Scripting.FileSystemObject")
Set oShell = CreateObject("WScript.Shell")
Set oSA    = CreateObject("Shell.Application")

' Directory this script lives in
sDir = oFSO.GetParentFolderName(WScript.ScriptFullName)

' ── Locate pythonw.exe (no-console Python) ────────────────────────────────────
Dim sPythonW
sPythonW = ""

' 1. Try "where pythonw" directly
Dim oExec, sOut
On Error Resume Next
Set oExec = oShell.Exec("cmd /c where pythonw 2>nul")
sOut = Trim(oExec.StdOut.ReadAll)
On Error GoTo 0

If sOut <> "" Then
    ' Take the first line in case multiple hits
    sPythonW = Split(sOut, vbCrLf)(0)
End If

' 2. Derive from python.exe location
If sPythonW = "" Or Not oFSO.FileExists(sPythonW) Then
    On Error Resume Next
    Set oExec = oShell.Exec("cmd /c where python 2>nul")
    sOut = Trim(oExec.StdOut.ReadAll)
    On Error GoTo 0
    If sOut <> "" Then
        Dim sPyDir
        sPyDir    = oFSO.GetParentFolderName(Split(sOut, vbCrLf)(0))
        sPythonW  = sPyDir & "\pythonw.exe"
    End If
End If

' 3. Try the py launcher to find Python home
If sPythonW = "" Or Not oFSO.FileExists(sPythonW) Then
    On Error Resume Next
    Set oExec = oShell.Exec("cmd /c py -c ""import sys,os;print(os.path.dirname(sys.executable))"" 2>nul")
    sOut = Trim(oExec.StdOut.ReadAll)
    On Error GoTo 0
    If sOut <> "" Then
        sPythonW = sOut & "\pythonw.exe"
    End If
End If

If sPythonW = "" Or Not oFSO.FileExists(sPythonW) Then
    MsgBox "Could not find pythonw.exe." & vbCrLf & vbCrLf & _
           "Make sure Python is installed and added to PATH.", _
           vbCritical, "NULLFRAME — Launcher Error"
    WScript.Quit 1
End If

' ── Install customtkinter silently (hidden window) ────────────────────────────
oShell.Run """" & sPythonW & """ -m pip install -q customtkinter", 0, True

' ── Launch app elevated, no console window ────────────────────────────────────
' ShellExecute with "runas" triggers UAC and opens as Administrator.
' Window style 1 = normal window (the GUI itself is visible; no black CMD box).
oSA.ShellExecute sPythonW, """" & sDir & "\nullframe.py""", sDir, "runas", 1
