Public Class FrmSplash
	Private Sub LBLSplash_TextChanged(sender As Object, e As EventArgs) Handles LBLSplash.TextChanged
		Trace.WriteLine("SpashText: " & LBLSplash.Text)
	End Sub
End Class