Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Text.RegularExpressions
Imports System.Drawing.Bitmap


Public Class Form1
    Dim configjson = Application.StartupPath + "\Shadowsocks\config.json"

    ' timing thread 
    Dim sw As New Stopwatch
    Dim TimingThread = New Thread(AddressOf ShowMessageTread)
    Public Structure threadparam ' the parameter to the "xml2srt" thread
        Public msg As String
    End Structure
    Private Delegate Sub trigger(ByRef str As String)
    Private Sub ShowMessageTread(ByVal parampass As Object)
        Dim parampassed = CType(parampass, threadparam)
        Dim jobname As String = parampassed.msg
        Me.Invoke(New trigger(AddressOf updateform), jobname)
        Thread.CurrentThread.Abort()
    End Sub
    Private Sub updateform(ByRef jobname As String)
        msg.Text += jobname.PadLeft(40, " ") + " used " + sw.Elapsed.Milliseconds.ToString.PadRight(4, " ") + "ms" + vbCrLf
    End Sub
    'timing thread ends
    Private Sub ShowStopWatch(ByVal jobname As String)
        'Dim echojob As threadparam
        'echojob.msg = msg
        'Dim TimingThread = New Thread(AddressOf ShowMessageTread)
        'TimingThread.start(echojob)
        msg.Text += jobname.PadRight(40, " ") + " used " + sw.Elapsed.Milliseconds.ToString.PadLeft(8, " ") + "ms" + vbCrLf
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        'msg output initialise
        msg.Text = vbNullString
        'timing thread initialize
        sw.Start()

        'backup old config file
        ShowStopWatch("Backing up config")
        If File.Exists(configjson) Then
            File.Copy(configjson, configjson + ".backup", True)
            File.Delete(configjson)
        Else
            If Not File.Exists(Application.StartupPath + "\GoAgentPlus.exe") Then
                MsgBox("Please Run me again in the GoAgentPlus folder!" + vbCrLf + "Download at https://goagentplus.com/")
                End
            End If
        End If

            'fetching initialize
            Dim json As String = vbNullString
            'fetch the json(server info that contains qr code urls)
            Try
                Dim jsonrequest As Net.HttpWebRequest = Net.WebRequest.Create("https://shadowsocks.net/api")
                'hwrequest.Accept =
                jsonrequest.AllowAutoRedirect = True
                'hwrequest.UserAgent = 
                jsonrequest.Timeout = 5000
                jsonrequest.Method = "GET"
                Dim jsonresponse As Net.HttpWebResponse = jsonrequest.GetResponse()
                If jsonresponse.StatusCode = Net.HttpStatusCode.OK Then
                    Dim responseStream As StreamReader = New IO.StreamReader(jsonresponse.GetResponseStream())
                    json = responseStream.ReadToEnd()
                End If
                jsonresponse.Close()
            Catch
                Dim ex As New Exception
                MsgBox(ex.Message)
            End Try
            ShowStopWatch("Fetching json")

            'Read json
            'Dim sr As New StreamReader("G:\shadowsocks-gui-source\SSG\SSG\api.json")
            If json = vbNullString Then Exit Sub 'exit when it fails to fetch json
            'extract and save server items json to array "servers"
            Dim servers(255) As String
            Dim QRurl(255) As String
            Dim Countries(255) As String
            Dim QRimg(255) As Bitmap
            Dim SSconfigRaw(255) As String
            Dim SSconfigstring(255) As String
            Dim SSconfigParam(3) As String
            'Dim serverlist(255) As Server
            'insert a # mark if there's no servers available
            servers(0) = "#"
            QRurl(0) = "#"
            Dim pattern As String = "\{[^\}]+\}"
            Dim count As Integer = 0
            'save server to array respectively
            For Each m As Match In Regex.Matches(json, pattern) 'static use of regex
                servers(count) = m.Value
                count += 1
            Next
            If servers(0) = "#" Then Exit Sub 'exit when it contains no server.
            'Parse qr code image urls from the server json array if the servers are online
            Dim urlcount As Integer = 0
            For index = 0 To count - 1
                Dim isOnline As New Regex("""online"": 1")
                Dim online As Match = isOnline.Match(servers(index))
                'if the server is online
                If online.Success Then
                    'Parse the qr code image url
                    Dim ParseURL As New Regex("qr/\d+\.png")
                    Dim url As Match = ParseURL.Match(servers(index))
                    QRurl(urlcount) = url.Value
                    'Parse the country where the server locates in
                    Dim ParseCountry As New Regex("(?<=""country"": "")\w+(?="", "")")
                    Dim Country As Match = ParseCountry.Match(servers(index))
                    Countries(urlcount) = Country.Value
                    urlcount += 1
                End If
            Next
            urlcount -= 1 ' THE FACTUAL NUMBER is urlcount-1 because an extra 1 will be added to urlcount when the the match ends.
            If urlcount = -1 Then Exit Sub 'exit when it contains no qrcode.
            'list url
            Dim out As String = vbNullString
            For index = 0 To urlcount ' THE FACTUAL NUMBER
                QRurl(index) = "http://shadowsocks.net/media/" + QRurl(index)
                out += QRurl(index) + vbCrLf
            Next
            ShowStopWatch("Parsing json")

            'Download QR code images
            'Try
            Dim hwrequest As Net.HttpWebRequest
            Dim hwresponse As Net.HttpWebResponse
            Dim readCode As New ZXing.BarcodeReader()
            For index = 0 To urlcount
                hwrequest = Net.WebRequest.Create(QRurl(index).Trim)
                'hwrequest.Accept =
                hwrequest.AllowAutoRedirect = True
                'hwrequest.UserAgent = 
                hwrequest.Timeout = 10000
                hwrequest.Method = "GET"
                ShowStopWatch("Fetching QR images " + index.ToString + "/" + urlcount.ToString)
                hwresponse = hwrequest.GetResponse()
                If hwresponse.StatusCode = Net.HttpStatusCode.OK Then
                    Dim bitmap As New Bitmap(hwresponse.GetResponseStream)
                    ShowStopWatch("Parsing  QR images " + index.ToString + "/" + urlcount.ToString)
                    'decode QR image
                    Dim result = readCode.Decode(bitmap)
                    SSconfigRaw(index) = CStr(result.Text)
                End If
            Next
            hwresponse.Close()
            'Catch ex As Exception
            '    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            'End Try

            For index = 0 To urlcount
                'Parsing QR code
                ' strip ss://
                pattern = "(?<=ss://).*"
                For Each m As Match In Regex.Matches(SSconfigRaw(index), pattern) 'static use of regex
                    SSconfigRaw(index) = m.Value
                Next
                ' convert to a string
                If Not SSconfigRaw(index).Length Mod 4 = 0 Then
                    ' the input string  has to be zero or a multiple of 4.
                    SSconfigRaw(index) += StrDup(4 - SSconfigRaw(index).Length Mod 4, "=") 'append = to invalid-length string
                End If
                'decode base64
                SSconfigstring(index) = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(SSconfigRaw(index)))
            Next

            'Write to config.json
            Dim UTF8WithoutBom As New System.Text.UTF8Encoding(False)
            Dim jsonwriter As New StreamWriter(configjson, False, UTF8WithoutBom)
            Dim ServerHistory As String = vbNullString
            Dim servercount As Integer = -1
            'a new record
            jsonwriter.Write("[")
            'parse param
            For index = 0 To urlcount
                pattern = "^(\w+-){0,}\w+(?=:.*@)|(?<=:).*(?=@)|(?<=@)((25[0-5]|2[0-4]\d|[0-1]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[0-1]?\d\d?)(?=:)|(?<=:)\d{1,5}$"
                Dim matchedtime As Integer = 0
                Dim method As String = vbNullString
                Dim password As String = vbNullString
                Dim server As String = vbNullString
                Dim server_port As String = vbNullString
                For Each m As Match In Regex.Matches(SSconfigstring(index), pattern) 'static use of regex
                    If m.Groups.Count = 5 Then 'correct match which contains encrption, password, ip, port
                        Select Case matchedtime
                            Case Is = 0 'encryption
                                method = m.Value
                            Case Is = 1 'password
                                password = m.Value
                            Case Is = 2 'ipaddress
                                server = m.Value
                            Case Is = 3 'port
                                server_port = m.Value
                            Case Else 'regex match error
                                'some code
                                '"Invalid shadowsocks config url!"
                                Exit Select
                        End Select
                        matchedtime += 1
                    Else
                        Exit For 'skip the invalid config url and search on the next
                    End If
                Next
                ' write new params to file
                servercount += 1
                jsonwriter.Write("{""method"":""" + method + """,")
            jsonwriter.Write("""name"":""" + Countries(index) + "-" + server + ":" + server_port + """,")
                jsonwriter.Write("""origin"":""local"",")
                jsonwriter.Write("""password"":""" + password + """,")
                jsonwriter.Write("""server"":""" + server + """,")
                jsonwriter.Write("""server_port"":""" + server_port + """}")
                If Not index = urlcount Then
                    'end of a record
                    jsonwriter.Write(",")
                Else
                    'end of the records
                    jsonwriter.WriteLine("]")
                End If
                matchedtime = 0 'initialize for the next SSconfigstring
                method = vbNullString
                password = vbNullString
                server = vbNullString
                server_port = vbNullString
            Next
            jsonwriter.Close()
            msg.Text += "==========================" + vbCrLf
            msg.Text += "Totally".PadRight(40, " ") + " used " + sw.Elapsed.TotalSeconds.ToString.PadLeft(8, " ") + "s" + vbCrLf
            Console.Write(out)
            out = vbNullString
            Console.WriteLine("==========SSconfigstring==========")
            For index = 0 To urlcount
                out += SSconfigstring(index) + vbCrLf
            Next
            Console.Write(out)
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Height = 600
        Me.Width = 500
        'msg.TextAlign = ContentAlignment.TopLeft
        'TimingThread.name = "stopwatch"
    End Sub
End Class
