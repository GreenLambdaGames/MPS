using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Net;
using System;

public class GameLauncher : MonoBehaviour
{
	public string gameURL = "https://example.com/game.zip"; // URL to the remote zip containing all the binaries
	public string remoteVersionURL = "https://example.com/remoteVersion.txt"; // URL to the text file containing the remote version
	public string localVersionPath = "Assets/StreamingAssets/localVersion.txt"; // Path to the local version file
	public string binaryPath = "Assets/StreamingAssets/Game.exe"; // Path to the binary file
	public Button playButton; // Reference to the play button in the UI

	private string localVersion;
	private string remoteVersion;
	private bool isUpdating;

	void Start ()
	{
		playButton.interactable = false; // Disable the button initially
		playButton.onClick.AddListener(OnPlayButtonClick);
		StartCoroutine(Initialize());
	}

	IEnumerator Initialize ()
	{
		// Load the local version
		localVersion = LoadLocalVersion();

		// Fetch the remote version
		yield return StartCoroutine(FetchRemoteVersion());

		// Compare versions and proceed accordingly
		if (remoteVersion == localVersion)
		{
			// Versions match, enable the play button
			playButton.interactable = true;
		}
		else
		{
			// Versions differ, initiate patching
			isUpdating = true;
			playButton.interactable = false;
			playButton.GetComponentInChildren<Text>().text = "Patching...";
			yield return StartCoroutine(DownloadGame());
			SaveLocalVersion(remoteVersion);
			isUpdating = false;
			playButton.interactable = true;
			playButton.GetComponentInChildren<Text>().text = "Play";
		}
	}

	string LoadLocalVersion ()
	{
		if (File.Exists(localVersionPath))
		{
			return File.ReadAllText(localVersionPath);
		}
		else
		{
			Debug.LogWarning("Local version file not found. Assuming initial version.");
			return "0.0.0";
		}
	}

	IEnumerator FetchRemoteVersion ()
	{
		using (var webClient = new WebClient())
		{
			var downloadTask = webClient.DownloadStringTaskAsync(remoteVersionURL);
			yield return new WaitUntil(() => downloadTask.IsCompleted);

			if (downloadTask.Exception != null)
			{
				Debug.LogError("Failed to fetch remote version: " + downloadTask.Exception.Message);
				yield break;
			}

			remoteVersion = downloadTask.Result.Trim();
		}
	}

	IEnumerator DownloadGame ()
	{
		using (var webClient = new WebClient())
		{
			var downloadTask = webClient.DownloadFileTaskAsync(gameURL, binaryPath);
			yield return new WaitUntil(() => downloadTask.IsCompleted);

			if (downloadTask.Exception != null)
			{
				Debug.LogError("Failed to download game: " + downloadTask.Exception.Message);
				yield break;
			}

			Debug.Log("Game downloaded successfully.");
		}
	}

	void SaveLocalVersion (string version)
	{
		File.WriteAllText(localVersionPath, version);
		Debug.Log("Local version saved: " + version);
	}

	void OnPlayButtonClick ()
	{
		if (isUpdating)
		{
			// Game is updating, do nothing when the button is clicked
			return;
		}

		// Execute the binary
		System.Diagnostics.Process.Start(binaryPath);

		// Close the Unity application
		Application.Quit();
	}
}