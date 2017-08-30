Public Class debugWindow

    Public Sub setimage(ByVal im1 As BitmapSource, ByVal im2 As BitmapSource, ByVal distance As Double)
        Image1.Source = im1
        Image2.Source = im2

        Image1.UpdateLayout()
        Image2.UpdateLayout()

        Me.Title = distance.ToString
    End Sub

    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        RenderOptions.SetBitmapScalingMode(Image1, BitmapScalingMode.NearestNeighbor)
        RenderOptions.SetBitmapScalingMode(Image2, BitmapScalingMode.NearestNeighbor)
    End Sub
End Class
