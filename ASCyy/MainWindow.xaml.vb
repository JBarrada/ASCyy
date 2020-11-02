Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks

Class MainWindow

    Class CharBitmap
        Public c As String
        Public width As Double
        Public height As Double

        Public widthData As Integer
        Public heightData As Integer
        Public data() As Double

        Public Function GetBrightness(ByVal x As Integer, ByVal y As Integer)
            If x >= 0 And x < widthData And y >= 0 And y < heightData Then
                Return data((y * widthData) + x)
            Else
                Return 255.0
            End If
        End Function
    End Class

    Public charBitmaps As New List(Of CharBitmap)
    Public charMaxWidth As Double = 0
    Public charMaxHeight As Double = 0

    'Public charMapFont As Typeface = New Typeface("Segoe UI")
    'Public charMapFont As Typeface = New Typeface("Tahoma")
    Public charMapFont As Typeface = New Typeface("Consolas")
    Public charMapFontSize As Integer = 12

    Public charLines As Integer = 25
    Dim imageStringLines(charLines - 1) As String

    Public taskCount As Integer = 0
    Public taskLock As New Object

    Dim WithEvents convertImage As New BitmapImage
    Dim convertImageData() As Double
    Dim convertImageWidth As Integer
    Dim convertImageHeight As Integer

    'http://i.imgur.com/0L5594g.jpg
    'https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_272x92dp.png
    'http://www.iconsdb.com/icons/preview/purple/circle-xxl.png

    Dim convertOnce As Boolean = False

    Public Sub UpdateCharBitmaps()
        charBitmaps.Clear()
        charMaxWidth = 0
        charMaxHeight = 0

        Dim charList As New List(Of Integer)
        charList.AddRange(Enumerable.Range(32, 126 - 32))
        'charList.AddRange(Enumerable.Range(128, 254 - 128))

        For Each i In charList
            Dim currentCharFT As New FormattedText(Chr(i).ToString, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, charMapFont, charMapFontSize, Brushes.Black)

            If currentCharFT.WidthIncludingTrailingWhitespace <> 0 Then
                Dim currentCharTB As New TextBlock
                currentCharTB.Text = Chr(i)
                currentCharTB.Margin = New Thickness(0)
                currentCharTB.Padding = New Thickness(0)
                currentCharTB.Width = currentCharFT.WidthIncludingTrailingWhitespace
                currentCharTB.Height = currentCharFT.Height
                currentCharTB.UpdateLayout()

                currentCharTB.Arrange(New Rect(0, 0, currentCharTB.Width, currentCharTB.Height))
                Dim currentCharBMP As New RenderTargetBitmap(currentCharTB.Width, currentCharTB.Height, 96, 96, PixelFormats.Default)
                currentCharBMP.Render(currentCharTB)
                currentCharBMP.Freeze()

                Dim charImageBytesPerPixel As Integer = currentCharBMP.Format.BitsPerPixel / 8
                Dim charImageStride As Integer = (currentCharBMP.PixelWidth * currentCharBMP.Format.BitsPerPixel + 7) / 8
                Dim charImageData((currentCharBMP.PixelHeight * charImageStride) - 1) As Byte
                currentCharBMP.CopyPixels(charImageData, charImageStride, 0)

                Dim currentCharBitmap As New CharBitmap
                currentCharBitmap.c = Chr(i).ToString
                currentCharBitmap.width = currentCharFT.WidthIncludingTrailingWhitespace
                currentCharBitmap.height = currentCharFT.Height
                currentCharBitmap.widthData = currentCharBMP.PixelWidth
                currentCharBitmap.heightData = currentCharBMP.PixelHeight
                ReDim currentCharBitmap.data((currentCharBitmap.widthData * currentCharBitmap.heightData) - 1)

                For y As Integer = 0 To currentCharBMP.PixelHeight - 1
                    For x As Integer = 0 To currentCharBMP.PixelWidth - 1
                        currentCharBitmap.data((y * currentCharBitmap.widthData) + x) = GetPerceivedBrightness(GetPixel(charImageData, charImageBytesPerPixel, charImageStride, x, y))
                    Next
                Next

                charBitmaps.Add(currentCharBitmap)

                If currentCharBitmap.widthData > charMaxWidth Then
                    charMaxWidth = currentCharBitmap.widthData
                End If
                If currentCharBitmap.heightData > charMaxHeight Then
                    charMaxHeight = currentCharBitmap.heightData
                End If
            End If
        Next
    End Sub

    Sub convertImageLoaded() Handles convertImage.DownloadCompleted, convertImage.Changed
        Dim convertImageBS As BitmapSource = New FormatConvertedBitmap(convertImage, PixelFormats.Default, Nothing, 0)
        convertImageWidth = convertImageBS.PixelWidth
        convertImageHeight = convertImageBS.PixelHeight
        Dim convertImageBytesPerPixel As Integer = convertImageBS.Format.BitsPerPixel / 8
        Dim convertImageStride As Integer = (convertImageWidth * convertImageBS.Format.BitsPerPixel + 7) / 8
        Dim convertImageDataRaw((convertImageHeight * convertImageStride) - 1) As Byte
        convertImageBS.CopyPixels(convertImageDataRaw, convertImageStride, 0)

        ReDim convertImageData((convertImageWidth * convertImageHeight) - 1)

        For y As Integer = 0 To convertImageHeight - 1
            For x As Integer = 0 To convertImageWidth - 1
                convertImageData((y * convertImageWidth) + x) = GetPerceivedBrightness(GetPixel(convertImageDataRaw, convertImageBytesPerPixel, convertImageStride, x, y))
            Next
        Next

        'Dim t As New Thread(AddressOf ConvertThread)
        't.Start()

        convertLinear()
        'ConvertThread()
    End Sub

    Function GetPerceivedBrightness(ByVal c As Color) As Double
        Dim alphaDouble As Double = CDbl(c.A) / 255.0
        Dim R As Double = ((255.0 * (1.0 - alphaDouble)) + (CDbl(c.R) * alphaDouble))
        Dim G As Double = ((255.0 * (1.0 - alphaDouble)) + (CDbl(c.G) * alphaDouble))
        Dim B As Double = ((255.0 * (1.0 - alphaDouble)) + (CDbl(c.B) * alphaDouble))

        Return Math.Sqrt((0.241 * (R * R)) + (0.691 * (G * G)) + (0.068 * (B * B)))
    End Function

    Function GetPixel(ByVal pixelData() As Byte, ByVal bytesPerPixel As Integer, ByVal stride As Integer, ByVal x As Integer, ByVal y As Integer) As Color
        Dim offset As Integer = (y * stride) + (x * bytesPerPixel)
        Dim c As Color = Color.FromArgb(pixelData(offset + 3), pixelData(offset + 1), pixelData(offset + 1), pixelData(offset + 0))
        Return c
    End Function

    Sub ConvertThread()
        Dispatcher.Invoke(Sub() TextBox1.FontFamily = charMapFont.FontFamily)
        Dispatcher.Invoke(Sub() TextBox1.FontSize = charMapFontSize)

        Dim taskFactory As New TaskFactory


        For i As Integer = 0 To charLines - 1
            imageStringLines(i) = ""
        Next

        taskCount = 0
        For lineY As Integer = 0 To charLines - 1
            Dim taskLine As Integer = lineY
            taskFactory.StartNew(Sub() ConvertLine(taskLine, imageStringLines(taskLine)))
        Next

        Dim imageStringJoin As String = ""
        While taskCount < charLines
            imageStringJoin = String.Join(vbNewLine, imageStringLines)
            Dispatcher.Invoke(Sub() TextBox1.Text = imageStringJoin)
            Thread.Sleep(1000)
        End While
        'imageStringJoin = String.Join(vbNewLine, imageStringLines)
        'Dispatcher.Invoke(Sub() TextBox1.Text = imageStringJoin)
    End Sub

    Sub convertLinear()
        TextBox1.FontFamily = charMapFont.FontFamily
        TextBox1.FontSize = charMapFontSize

        If convertOnce = True Then
            Exit Sub
        End If
        convertOnce = True

        For i As Integer = 0 To charLines - 1
            imageStringLines(i) = ""
        Next

        For lineY As Integer = 0 To charLines - 1
            ConvertLine(lineY, imageStringLines(lineY))
            TextBox1.Text += imageStringLines(lineY) & vbNewLine
            Console.WriteLine(lineY.ToString)
        Next
    End Sub

    Sub ConvertLine(ByVal line As Integer, ByRef outputString As String)
        outputString = ""
        Dim imageLinePixels As Double = convertImageHeight / charLines
        Dim imagePixelsCharPixel As Double = imageLinePixels / charBitmaps.First.height ' maybe try heightdata

        Dim imageYPos As Double = line * imageLinePixels
        Dim imageXPos As Double = 0

        While imageXPos < convertImageWidth
            Dim bestChar As CharBitmap = FindBestChar(imageXPos, imageYPos)
            outputString += bestChar.c
            imageXPos += bestChar.width * imagePixelsCharPixel
        End While

        SyncLock (taskLock)
            taskCount += 1
        End SyncLock
    End Sub

    Function FindBestChar(ByVal imageXPos As Integer, ByVal imageYPos As Integer) As CharBitmap
        Dim imageLinePixels As Double = convertImageHeight / charLines
        Dim imagePixelsCharPixel As Double = imageLinePixels / charBitmaps.First.height ' maybe try heightdata

        Dim imageBrightnessBitmap(charMaxWidth * charMaxHeight) As Double
        Dim imageTotalBrightness As Double = 0
        For y As Integer = 0 To charMaxHeight - 1
            For x As Integer = 0 To charMaxWidth - 1
                Dim imageBrightness As Double = 0
                Dim imageBrightnessCount As Integer = 0

                Dim imageOffsetXPos As Double = imageXPos + (x * imagePixelsCharPixel)
                Dim imageOffsetYPos As Double = imageYPos + (y * imagePixelsCharPixel)

                For oy As Integer = 0 To Math.Ceiling(imagePixelsCharPixel) - 1
                    For ox As Integer = 0 To Math.Ceiling(imagePixelsCharPixel) - 1
                        If Math.Floor(imageOffsetXPos + ox) < convertImageWidth And Math.Floor(imageOffsetYPos + oy) < convertImageHeight Then
                            imageBrightness += convertImageData((Math.Floor(imageOffsetYPos + oy) * convertImageWidth) + Math.Floor(imageOffsetXPos + ox)) ' Math.Floor(imageOffsetXPos + ox), Math.Floor(imageOffsetYPos + oy)))
                        Else
                            imageBrightness += 255.0
                        End If
                        imageBrightnessCount += 1
                    Next
                Next

                imageBrightness /= CDbl(imageBrightnessCount)
                imageBrightnessBitmap((y * charMaxWidth) + x) = imageBrightness
                imageTotalBrightness += imageBrightness
            Next
        Next
        imageTotalBrightness /= (charMaxWidth * charMaxHeight)

        If imageTotalBrightness = 255.0 Then
            Return charBitmaps.First ' space
        End If

        Dim bestChar As New CharBitmap
        Dim bestCharDistance As Double = 255.0
        For Each c In charBitmaps
            Dim currentDistance As Double = 0
            Dim currentDistanceCount As Integer = 0
            For y As Integer = 0 To c.heightData - 1
                For x As Integer = 0 To c.widthData - 1
                    Dim imageBrightness As Double = imageBrightnessBitmap((y * charMaxWidth) + x)

                    'currentDistance += Math.Abs(c.GetBrightness(x, y) - imageBrightness)
                    currentDistance += Math.Pow(Math.Abs(c.GetBrightness(x, y) - imageBrightness) / 255.0, 2)
                    currentDistanceCount += 1
                Next
            Next

            currentDistance /= CDbl(currentDistanceCount)

            If currentDistance < bestCharDistance Then
                bestCharDistance = currentDistance
                bestChar = c
            End If
        Next

        Return bestChar
    End Function

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Button1.Click
        UpdateCharBitmaps()

        'convertImage = New BitmapImage(New Uri(Clipboard.GetText))
        'convertImage = New BitmapImage(New Uri("http://www.iconsdb.com/icons/preview/purple/circle-xxl.png"))
        convertImage = New BitmapImage(New Uri("https://imgaz3.staticbg.com/thumb/large/oaupload/banggood/images/57/99/63b33f8a-923f-42ee-9b61-c7e6ee54e076.jpg"))
        'convertImage = New BitmapImage(New Uri("http://zdnet1.cbsistatic.com/hub/i/2015/09/01/cb834e24-18e7-4f0a-a9bf-4c2917187d3f/83bb139aac01023dbf3e55a3d1789ad8/google-new-logo.png"))
        'convertImage = New BitmapImage(New Uri("http://i.imgur.com/0L5594g.jpg"))
        'convertImage = New BitmapImage(New Uri("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQgfZeQfcPa39Zodu_eXAUs9xYjmRk5lBry4TWh5nCWiWLf7dka-g&s"))

        'convertImage = New BitmapImage(New Uri("C:\Users\Justin B\Pictures\tKagYho-.jpg"))
        'convertImage = New BitmapImage(New Uri("C:\Users\Justin B\Pictures\red-party-cups-solo.jpg"))
        'convertImageLoaded()
    End Sub
End Class
