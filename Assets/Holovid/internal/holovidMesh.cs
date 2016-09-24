using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//creates a sufficiently dense mesh for the displacement

public class holovidMesh : MonoBehaviour {

	public int meshResX = 200;
	public int meshResY = 200;
	[Tooltip ("The resolution of the entire movie texture.")]
	public Vector2 imageRes;
	[Tooltip ("The upper left pixel x/y of the color portion of the movie.")]
	public Vector2 rgbCoord;
	[Tooltip ("The pixel width/heigt of the color portion of the movie.")]
	public Vector2 rgbDim;

	public Material depthMovieMaterial;

	public UnityEngine.UI.Slider depthSlider;

	protected MovieTexture movie = null;
	protected AudioSource audioSrc = null;

	void Start()
	{
		if (!movie) 
		{
			Renderer r = GetComponent<Renderer>();
			if (r)
				movie = (MovieTexture)r.material.mainTexture;
		}

		if (movie) 
		{
			movie.Play ();
			audioSrc = GetComponent<AudioSource> ();
			audioSrc.clip = movie.audioClip;
			audioSrc.Play ();
		}
	}


	public void play()
	{
		if (movie)
			movie.Play();
		if (audioSrc)
			audioSrc.Play ();
	}
	public void stop()
	{
		if (movie) 
		{
			movie.Stop (); //this leaves it at the current frame. when what it should really do is reset to frame 1
			StartCoroutine("stopping");
			audioSrc.Stop ();
		}
	}
	public void pause()
	{
		if (movie)
			movie.Pause();
		if (audioSrc)
			audioSrc.Pause ();
	}

	IEnumerator stopping()
	{
		yield return new WaitForSeconds (.1f);
		movie.Play ();
		yield return new WaitForSeconds (.1f);
		movie.Pause ();//.should now be at frame 1

	}

	public void setDepth()
	{
		GetComponent<Renderer>().material.SetFloat("_Displacement", depthSlider.value);
	}


	void OnValidate()
	{
		meshResX = Mathf.Clamp (meshResX, 1, 255);
		meshResY = Mathf.Clamp (meshResY, 1, 254); //maximum vert count limit

		if (imageRes.x < 1)
			imageRes.x = 1;
		if (imageRes.y < 1)
			imageRes.y = 1;
		if (rgbDim.x < 1)
			rgbDim.x = 1;
		if (rgbDim.y < 1)
			rgbDim.y = 1;

		
		generateHolovidMesh (gameObject, 
			new Vector2 (-.5f, -.5f), //position of the mesh
			new Vector2 (rgbDim.x / rgbDim.y, 1f), //size of the mesh
			new Vector2(rgbCoord.x/imageRes.x, rgbCoord.y/imageRes.y), // uv pos 
			new Vector2 (rgbDim.x/imageRes.x, rgbDim.y/imageRes.y), //uv dims - send whole and let the shader do the work here.
			meshResX, meshResY); 
	}

	void generateHolovidMesh(GameObject g, Vector2 pos, Vector2 dims, Vector2 UVpos, Vector2 UVdims, int xTesselation, int yTesselation)
	{
		xTesselation = Mathf.Clamp (xTesselation, 1, 255);
		yTesselation = Mathf.Clamp (yTesselation, 1, 254);

		//do stufff

		List<Vector3> verts = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector2> uvs = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();

		//vert index
		for (int r = 0; r <= yTesselation; r++)
		{
			//lerp between the top left and bottom left, then lerp between the top right and bottom right, and save the vectors

			float rowLerpValue = (float)r / (float)yTesselation;

			Vector3 bottomLeft = new Vector3 (pos.x, pos.x + dims.y, 0f);
			Vector3 topRight = new Vector3 (pos.x + dims.x, pos.y, 0f);
			Vector3 bottomRight = new Vector3 (pos.x + dims.x, pos.y + dims.y, 0f);

			Vector3 cellLeft = Vector3.Lerp(pos, bottomLeft, rowLerpValue); //lerp between topleft/bottomleft
			Vector3 cellRight = Vector3.Lerp(topRight, bottomRight, rowLerpValue); //lerp between topright/bottomright

			for (int c = 0; c <= xTesselation; c++)
			{
				//Now that we have our start and end coordinates for the row, iteratively lerp between them to get the "columns"
				float columnLerpValue = (float)c / (float)xTesselation;

				//now get the final lerped vector
				Vector3 lerpedVector = Vector3.Lerp(cellLeft, cellRight, columnLerpValue);
				verts.Add(lerpedVector);

				//uvs
				//uvs.Add(new Vector2((float)c / (float)xTesselation, (float)r / yTesselation)); //0-1 code
				Vector2 uv = new Vector2 (
					             (float)c / (float)xTesselation, 
								(float)r / (float)yTesselation);
				uv.x *= UVdims.x;
				uv.y *= UVdims.y;
				uv += UVpos;
				uvs.Add (uv);

				normals.Add (Vector3.forward);
			}
		}

		//triangles
		//we only want < gridunits because the very last verts in bth directions don't need triangles drawn for them.
		for (int x = 0; x < xTesselation; x++)
		{
			for (int y = 0; y < yTesselation; y++)
			{
				triangles.Add(x + ((y + 1) * (xTesselation + 1)));
				triangles.Add((x + 1) + (y * (xTesselation + 1)));
				triangles.Add(x + (y * (xTesselation + 1))); //width in verts

				triangles.Add(x + ((y + 1) * (xTesselation + 1)));
				triangles.Add((x + 1) + (y + 1) * (xTesselation + 1));
				triangles.Add((x + 1) + (y * (xTesselation + 1)));
			}
		}


		Mesh newMesh = new Mesh();

		newMesh.SetVertices(verts);
		newMesh.SetTriangles(triangles, 0);
		newMesh.SetUVs(0, uvs);
		newMesh.SetNormals (normals);

		//newMesh.RecalculateBounds();
		//newMesh.RecalculateNormals();

		//now that we have the mesh ready to go lets put it in
		MeshFilter meshFilter = g.GetComponent<MeshFilter>();
		if (!meshFilter)
			meshFilter = g.AddComponent<MeshFilter> ();
		//MeshRenderer meshRenderer = g.GetComponent<MeshRenderer>();

		//HACK ALERT!
		newMesh.bounds = new Bounds(Vector3.zero, new Vector3(200f,200f,200f)); //just make huge bounds. always draw it, don't mess around here.

		meshFilter.sharedMesh = newMesh;
	}
}
