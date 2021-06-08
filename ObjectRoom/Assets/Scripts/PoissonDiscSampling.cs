using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiscSampling
{

	public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 100)
	{
		float cellSize = radius / Mathf.Sqrt(2);

		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(sampleRegionSize / 2);
		while (spawnPoints.Count > 0)
		{
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
				{
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					candidateAccepted = true;
					break;
				}
			}
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}

		}

		return points;
	}

	public static List<Vector2> GeneratePointsInRestrictedViewPort(float radius, Vector2 sampleRegionSize, int NumOfObj, GameObject camera, int numSamplesBeforeRejection = 100)
	{
		float cellSize = radius / Mathf.Sqrt(2);
		//making grid of spot sizes based on numOfObj, to make objects generated without over 
		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(sampleRegionSize / 2);
		Debug.Log("What we wish: " + NumOfObj);

		Camera cameraComponent = camera.GetComponent<Camera>();
		while (spawnPoints.Count > 0 && points.Count != NumOfObj + 1)
		{
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			int i;
			for (i = 0; i < numSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid) && IsInCameraViewport(candidate, cameraComponent,sampleRegionSize.x,sampleRegionSize.y))
				{
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					candidateAccepted = true;
					break;
				}
			}
			Debug.Log("Stopped at iteration: " + i);
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}
		}


		return points;
	}


	static bool IsInCameraViewport(Vector2 candidate, Camera cameraComponent, float SpawnAreaX, float SpawnAreaY)
	{
		Vector3 newPos = new Vector3(candidate.x - SpawnAreaX/2, 0, candidate.y - SpawnAreaY/2);
		// Vector3 viewPointNewPos = camera.GetComponent<Camera>().WorldToViewportPoint(newPos);
		Vector3 viewPointNewPos = cameraComponent.WorldToViewportPoint(newPos);

		return viewPointNewPos.z > 0 && viewPointNewPos.x > 0 && viewPointNewPos.x < 1 && viewPointNewPos.y > 0 && viewPointNewPos.y < 1;
	}

	static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
	{
		if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
		{
			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);
			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY - 2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++)
			{
				for (int y = searchStartY; y <= searchEndY; y++)
				{
					int pointIndex = grid[x, y] - 1;
					if (pointIndex != -1)
					{
						float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
						if (sqrDst < radius * radius)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
	}
}