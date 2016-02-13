Imports System.IO
Imports System.Reflection
Imports System.Diagnostics

Module Module1

    Sub Main()
        ' 実行ファイルのパスを得る。
        Dim exe_path As String = Assembly.GetExecutingAssembly().Location

        ' MyFM.slnがあるフォルダまでさかのぼる。
        Dim root_dir As String = Path.GetDirectoryName(exe_path)
        Do While Not File.Exists(root_dir + "\MyFM.sln")
            root_dir = Path.GetDirectoryName(root_dir)
        Loop

        ' プロジェクトファイルのリスト
        Dim project_file_list As String() = {"sample\Basic\App6\App6.xml", "MyFM\MyFM.xml", "sample\Basic\App5\App5.xml", "sample\Basic\App4\App4.xml", "sample\Basic\App1\App1.xml", "sample\Basic\App2\App2.xml", "sample\Basic\App3\App3.xml"}
        'Dim project_file_list As String() = { "sample\Basic\App4\App4.xml", "sample\Basic\App5\App5.xml"}

        For Each project_file In project_file_list

            ' プロジェクトファイルの絶対パス
            Dim project_file_path As String = root_dir + "\" + project_file

            ' プロジェクトファイルからプロジェクトを作る。
            Dim prj1 As TProject = TProject.MakeProject(project_file_path)
            prj1.OutputSourceFile()
        Next
    End Sub

End Module
