﻿''' <summary>
''' BackgroundWorker-Class to raise the RunWorkerCompleted-Event manually.
''' </summary>
Public Class BackgroundWorkerSyncable
	Inherits System.ComponentModel.BackgroundWorker


#Region "------------------------------------------------- Sync Required -------------------------------------------------------"

	Private _SyncRequired As Boolean = True

	Public ReadOnly Property SyncRequired As Boolean
		Get
			Return _SyncRequired
		End Get
	End Property

#End Region ' Sync Required

#Region "------------------------------------------------- MyBaseRelated -------------------------------------------------------"

	''' =========================================================================================================
	''' <summary>
	''' Gets a value indicating whether the BackgroundWorker is running an asynchronous operation or delaying the
	''' RunWorkerCompleted-Event.
	''' </summary>
	''' <returns></returns>
	''' <remarks></remarks>
	Shadows ReadOnly Property isBusy As Boolean
		Get
			If MyBase.IsBusy Or _ResultCache IsNot Nothing Then
				Return True
			Else
				Return False
			End If
		End Get
	End Property

	''' =========================================================================================================
	''' <summary>
	''' Starts the Bachgroundworker async.
	''' </summary>
	''' <remarks></remarks>
	''' <exception cref="InvalidOperationException">Occurs if you try to start a new thread, without syncing the 
	''' previous results.</exception>
	Public Shadows Sub RunWorkerAsync(Obj As Object, Optional ByVal SyncRequired As Boolean = True)
		If _ResultCache IsNot Nothing Then Throw New InvalidOperationException("Starting is not allowed while a previous result is cached.")
		_SyncRequired = SyncRequired
		_SyncTimeOut = False
		MyBase.RunWorkerAsync(Obj)
	End Sub

	Protected Overrides Sub OnRunWorkerCompleted(e As System.ComponentModel.RunWorkerCompletedEventArgs)
		If _SyncTimeOut Then Exit Sub
		If SyncRequired Then
			_ResultCache = e
		Else
			MyBase.OnRunWorkerCompleted(e)
		End If
	End Sub

#End Region  ' MyBase-Related

#Region "------------------------------------------------- Sync Members --------------------------------------------------------"

	''' <summary>
	''' caches the result of the DoWork.Event, when _SyncReqired = True  
	''' </summary>
	''' <remarks></remarks>
	Private _ResultCache As System.ComponentModel.RunWorkerCompletedEventArgs = Nothing

	Private _SyncTimeOut As Boolean

	''' <summary>
	''' Raises the RunWorkerCompled-Event, when the process in me.DoWork has been
	''' finished. Otherwise this function starts a Application.DoEvents-Loop on 
	''' the calling thread until the BackgroundWorker has finished or an the given 
	''' Time has elapsed.
	''' </summary>
	''' <param name="Timeout">Time to wait in seconds, before the timeout occurs. After 
	''' a Timeout the RunWorkerCompleted-Event will not trigger and the Data processed 
	''' by the Backgroundthread is lost!</param>
	''' <remarks>If a Timeout occurs, CancelAsnyc() is called.</remarks>
	''' <exception cref="TimeoutException">Occurs if the given time has elapsed.</exception>
	''' <exception cref="Exception">Rethrows all exceptions occured in me.DoWork!</exception>
	Public Sub WaitToFinish(Optional ByVal Timeout As Integer = 0)
		' Declare new Stopwatch Instance for measering time 
		Dim sw As New Stopwatch
		' Start it, when a timeout is set.
		If Timeout > 0 Then sw.Start()

		' Wait until the Background worker has done his work.
		Do While MyBase.IsBusy
			' =============================== Timeout ==================================
			If sw.ElapsedMilliseconds > Timeout * 1000 Then
				'Stop the Watch
				sw.Stop()
				' Set marker -> This will prevent triggering the RunWorkerCompleted
				' For this Current process.
				_SyncTimeOut = True
				' Cancel the Backgroundwork
				If Me.WorkerSupportsCancellation Then Me.CancelAsync()
				' Throw an Exception
				Throw New TimeoutException("Timeout occured during Syncing ThreadResults.")
			End If
			' Don't block the calling thread.
			Application.DoEvents()
		Loop
		If _ResultCache Is Nothing Then Exit Sub
		' if an Error occured in BGW.DoWork the Error is "rethrown" here.
		If Me._ResultCache.Error IsNot Nothing Then Throw Me._ResultCache.Error

		MyBase.OnRunWorkerCompleted(_ResultCache)
		CancelSync()
	End Sub

	''' <summary>
	''' Cancels the current Syncing and deletes all fetched data.
	''' </summary>
	''' <remarks></remarks>
	Public Sub CancelSync()
		_ResultCache = Nothing
		_SyncRequired = False
	End Sub

#End Region  ' Sync-Members

End Class