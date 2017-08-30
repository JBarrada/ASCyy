Imports System.IO
Imports System.Windows.Media.Media3D

Public Class BMP
    Private imageData() As Byte

    Public Width As Integer
    Public Height As Integer

    Public Sub New(ByVal width As Integer, ByVal height As Integer)
        Me.Width = width
        Me.Height = height

        ReDim imageData((Me.Width * Me.Height * 3) - 1)
    End Sub

    Sub SaveBMP(ByVal filename As String)
        Dim bmp_file_size As Integer = 53 + (Me.Height * Me.Width * 3)

        Dim bmp_f_header(14 - 1) As Byte
        bmp_f_header(0) = 66
        bmp_f_header(1) = 77
        bmp_f_header(2) = (bmp_file_size >> 0) And &HFF
        bmp_f_header(3) = (bmp_file_size >> 8) And &HFF
        bmp_f_header(4) = (bmp_file_size >> 16) And &HFF
        bmp_f_header(5) = (bmp_file_size >> 24) And &HFF
        bmp_f_header(10) = 54

        Dim bmp_i_header(40 - 1) As Byte
        bmp_i_header(0) = 40
        bmp_i_header(4) = (Me.Width >> 0) And &HFF
        bmp_i_header(5) = (Me.Width >> 8) And &HFF
        bmp_i_header(6) = (Me.Width >> 16) And &HFF
        bmp_i_header(7) = (Me.Width >> 24) And &HFF
        bmp_i_header(8) = (Me.Height >> 0) And &HFF
        bmp_i_header(9) = (Me.Height >> 8) And &HFF
        bmp_i_header(10) = (Me.Height >> 16) And &HFF
        bmp_i_header(11) = (Me.Height >> 24) And &HFF
        bmp_i_header(12) = 1
        bmp_i_header(14) = 24

        Dim bmp_pad As Integer = (4 - (Me.Width * 3) Mod 4) Mod 4

        Dim f As New FileStream(filename, FileMode.Create)
        f.Write(bmp_f_header, 0, 14)
        f.Write(bmp_i_header, 0, 40)

        For i As Integer = 0 To (Me.Height - 1)
            f.Write(imageData, Me.Width * (Me.Height - i - 1) * 3, Me.Width * 3)
            For pad As Integer = 0 To (bmp_pad - 1)
                f.WriteByte(0)
            Next
        Next
        f.Close()
    End Sub

    Public Sub FXAA()
        Dim fxaaData((Me.Width * Me.Height * 3) - 1) As Byte

        Dim fxaaReduceMin As Double = (1.0 / 128.0)
        Dim fxaaReduceMul As Double = (1.0 / 8.0)
        Dim fxaaSpanMax As Double = (8.0)

        For x As Integer = 0 To Me.Width - 1
            For y As Integer = 0 To Me.Height - 1
                Dim rgbNW As Vector3D = GetPixelVector(x - 1, y + 1)
                Dim rgbNE As Vector3D = GetPixelVector(x + 1, y + 1)
                Dim rgbSW As Vector3D = GetPixelVector(x - 1, y - 1)
                Dim rgbSE As Vector3D = GetPixelVector(x + 1, y - 1)
                Dim rgbM As Vector3D = GetPixelVector(x, y)

                Dim luma As New Vector3D(0.299, 0.587, 0.114)

                Dim lumaNW As Double = Vector3D.DotProduct(rgbNW, luma)
                Dim lumaNE As Double = Vector3D.DotProduct(rgbNE, luma)
                Dim lumaSW As Double = Vector3D.DotProduct(rgbSW, luma)
                Dim lumaSE As Double = Vector3D.DotProduct(rgbSE, luma)
                Dim lumaM As Double = Vector3D.DotProduct(rgbM, luma)

                Dim lumaMin As Double = Math.Min(Math.Min(Math.Min(Math.Min(lumaNW, lumaNE), lumaSW), lumaSE), lumaM)
                Dim lumaMax As Double = Math.Max(Math.Max(Math.Max(Math.Max(lumaNW, lumaNE), lumaSW), lumaSE), lumaM)

                Dim dirX As Double = -((lumaNW + lumaNE) - (lumaSW + lumaSE))
                Dim dirY As Double = ((lumaNW + lumaSW) - (lumaNE + lumaSE))

                Dim dirReduce As Double = Math.Max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * fxaaReduceMul), fxaaReduceMin)

                Dim rcpDirMin As Double = 1.0 / (Math.Min(Math.Abs(dirX), Math.Abs(dirY)) + dirReduce)

                dirX = Math.Min(fxaaSpanMax, Math.Max(-fxaaSpanMax, dirX * rcpDirMin))
                dirY = Math.Min(fxaaSpanMax, Math.Max(-fxaaSpanMax, dirY * rcpDirMin))

                Dim rgbAa As Vector3D = GetPixelVector(x + dirX * (1.0 / 3.0 - 0.5), y + dirY * (1.0 / 3.0 - 0.5))
                Dim rgbAb As Vector3D = GetPixelVector(x + dirX * (2.0 / 3.0 - 0.5), y + dirY * (2.0 / 3.0 - 0.5))
                Dim rgbA As Vector3D = 0.5 * (rgbAa + rgbAb)

                Dim rgbBa As Vector3D = GetPixelVector(x + dirX * -0.5, y + dirY * -0.5)
                Dim rgbBb As Vector3D = GetPixelVector(x + dirX * 0.5, y + dirY * 0.5)
                Dim rgbB As Vector3D = rgbA * 0.5 + 0.25 * (rgbBa + rgbBb)

                Dim lumaB As Double = Vector3D.DotProduct(rgbB, luma)

                If ((lumaB < lumaMin) Or (lumaB > lumaMax)) Then
                    SetPixelVector(fxaaData, x, y, rgbA)
                Else
                    SetPixelVector(fxaaData, x, y, rgbB)
                End If
            Next
        Next

        Array.Copy(fxaaData, imageData, fxaaData.Length)
    End Sub

    Public Sub GetPixel(ByVal x As Integer, ByVal y As Integer, ByRef r As Byte, ByRef g As Byte, ByRef b As Byte)
        If (x >= 0 And x < Me.Width) And (y >= 0 And y < Me.Height) Then
            Dim index As Integer = y * (Me.Width * 3) + (x * 3)
            b = imageData(index + 0)
            g = imageData(index + 1)
            r = imageData(index + 2)
        End If
    End Sub
    Public Function GetPixelVector(ByVal x As Integer, ByVal y As Integer) As Vector3D
        If (x >= 0 And x < Me.Width) And (y >= 0 And y < Me.Height) Then
            Dim index As Integer = y * (Me.Width * 3) + (x * 3)
            Return New Vector3D(imageData(index + 2) / 255.0, imageData(index + 1) / 255.0, imageData(index + 0) / 255.0)
        Else
            Return New Vector3D(0, 0, 0)
        End If
    End Function
    Public Function GetPixelVector(ByRef imageData() As Byte, ByVal x As Integer, ByVal y As Integer) As Vector3D
        If (x >= 0 And x < Me.Width) And (y >= 0 And y < Me.Height) Then
            Dim index As Integer = y * (Me.Width * 3) + (x * 3)
            Return New Vector3D(imageData(index + 2) / 255.0, imageData(index + 1) / 255.0, imageData(index + 0) / 255.0)
        Else
            Return New Vector3D(0, 0, 0)
        End If
    End Function
    Public Sub SetPixel(ByVal x As Integer, ByVal y As Integer, ByVal r As Byte, ByVal g As Byte, ByVal b As Byte)
        Dim index As Integer = y * (Me.Width * 3) + (x * 3)
        imageData(index + 0) = b
        imageData(index + 1) = g
        imageData(index + 2) = r
    End Sub
    Public Sub SetPixelVector(ByVal x As Integer, ByVal y As Integer, ByVal rgb As Vector3D)
        Dim index As Integer = y * (Me.Width * 3) + (x * 3)
        imageData(index + 0) = rgb.Z * 255.0
        imageData(index + 1) = rgb.Y * 255.0
        imageData(index + 2) = rgb.X * 255.0
    End Sub
    Public Sub SetPixelVector(ByRef imageData() As Byte, ByVal x As Integer, ByVal y As Integer, ByVal rgb As Vector3D)
        Dim index As Integer = y * (Me.Width * 3) + (x * 3)
        imageData(index + 0) = rgb.Z * 255.0
        imageData(index + 1) = rgb.Y * 255.0
        imageData(index + 2) = rgb.X * 255.0
    End Sub
End Class