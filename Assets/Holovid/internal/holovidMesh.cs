//David Lycan - Define UNITY_VERSION_PRE_5_2
#if UNITY_5_0 || UNITY_5_0_1 || UNITY_5_0_2 || UNITY_5_0_3 || UNITY_5_0_4 || UNITY_5_1 || UNITY_5_1_1 || UNITY_5_1_2 || UNITY_5_1_3 || UNITY_5_1_4 || UNITY_5_1_5
	#define UNITY_VERSION_PRE_5_2
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//creates a sufficiently dense mesh for the displacement

[ExecuteInEditMode]
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
	
	//David Lycan - Previous dimensions for material input
	private int graphicsShaderLevel = 0;
	private Vector4 previousDims = Vector4.zero;


	Shader currentShader;
	public Shader holovidShader;
	public Shader particleShader;
	public Shader particleAdditiveShader;
	public Shader particleFallbackShader;
	public Shader particleAdditiveFallbackShader;

	void Start()
	{
		if (Application.isPlaying)
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
	
	void Update()
	{
		//David Lycan - Detect whether current Graphics Shader Level supports Geometry shaders
		//manually set the fallbacks so we can detect that the fallback occured and adjust our geometry accordingly
		currentShader = GetComponent<Renderer>().sharedMaterial.shader;

		if (currentShader == particleShader && !particleShader.isSupported) 
		{
			GetComponent<Renderer> ().sharedMaterial.shader = particleFallbackShader;
			currentShader = particleFallbackShader;
		}

		if (currentShader == particleAdditiveShader && !particleAdditiveShader.isSupported) 
		{
			GetComponent<Renderer> ().sharedMaterial.shader = particleAdditiveFallbackShader;
			currentShader = particleAdditiveFallbackShader;
		}

		if (currentShader == particleFallbackShader && !particleFallbackShader.isSupported) 
		{
			GetComponent<Renderer> ().sharedMaterial.shader = holovidShader;
			currentShader = holovidShader;
		}
		if (currentShader == particleAdditiveFallbackShader && !particleAdditiveFallbackShader.isSupported) 
		{
			GetComponent<Renderer> ().sharedMaterial.shader = holovidShader;
			currentShader = holovidShader;
		}

		if (currentShader == particleShader || currentShader == particleAdditiveShader) 
			graphicsShaderLevel = 2;
		else if (currentShader == particleFallbackShader || currentShader == particleAdditiveFallbackShader) 
			graphicsShaderLevel = 1;
		else
			graphicsShaderLevel = 0;
			
		




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

		//David Lycan - Update the dimensions in the movie's material if necessary
		Vector4 newDims = new Vector4( 1.07333f * rgbDim.y / imageRes.x, 1.09f * rgbDim.y / imageRes.y, 1f, 1f);
		
		if (previousDims != newDims)
		{
			if (graphicsShaderLevel > 0)
			{
				depthMovieMaterial.SetVector("_Dims", newDims);
			}
			
			previousDims = newDims;
		}
		
		
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

//		int vertCount = (yTesselation * xTesselation) + 2; //+2 is inclusive since we need verts at the end of the count to complete the quads
//		if (graphicsShaderLevel == 1)
//			vertCount = vertCount * 4; //we need to account for our own verts here, since our GPU does not support the needed shader

		List<Vector3> verts = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector2> uvs = new List<Vector2>();
		//David Lycan - Added a second uv list
		List<Vector2> uv2s = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();


        //vert index
        Vector2 uv = new Vector2();
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
				uv.x = (float)c / (float)xTesselation;
                uv.y = (float)r / (float)yTesselation;

				uv.x *= UVdims.x;
				uv.y *= UVdims.y;
				uv += UVpos;
				uvs.Add (uv);

				normals.Add (Vector3.forward);
				
				//David Lycan - When the Graphics Shader Level does not support Geometry shaders add the extra vertices, uvs and normals necessary
				//				Also add triangles to form each billboard quad here
				if (graphicsShaderLevel == 1)
				{
					verts.Add(lerpedVector);
					verts.Add(lerpedVector);
					verts.Add(lerpedVector);
					
					uvs.Add (uv);
					uvs.Add (uv);
					uvs.Add (uv);
					
					uv2s.Add (new Vector2( 1f,  1f) );
					uv2s.Add (new Vector2(-1f,  1f) );
					uv2s.Add (new Vector2( 1f, -1f) );
					uv2s.Add (new Vector2(-1f, -1f) );
					
					normals.Add (Vector3.forward);
					normals.Add (Vector3.forward);
					normals.Add (Vector3.forward);
				}
			}
		}
		
		
		//triangles
		//we only want < gridunits because the very last verts in bth directions don't need triangles drawn for them.
		//David Lycan - When the Graphics Shader Level does support Geometry shaders or the default Holovid shader is being used then add triangles to form the video geometry here
		if (graphicsShaderLevel == 1)
		{
			for (int x = 0; x < xTesselation; x++)
			{
				for (int y = 1; y <= yTesselation; y++)
				{
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4 + 1);
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4 + 2);
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4);
					
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4 + 1);
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4 + 3);
					triangles.Add(x * 4 + y * (xTesselation + 1) * 4 + 2);
				}
			}
		}
		else
		{
			for (int x = 0; x < xTesselation; x++)
			{
				for (int y = 0; y < yTesselation; y++)
				{
					triangles.Add(x + ((y + 1) * (xTesselation + 1)));
					triangles.Add((x + 1) + (y * (xTesselation + 1)));
					triangles.Add(x + (y * (xTesselation + 1))); //width in verts

					if (graphicsShaderLevel != 2)
					{
						triangles.Add(x + ((y + 1) * (xTesselation + 1)));
						triangles.Add((x + 1) + (y + 1) * (xTesselation + 1));
						triangles.Add((x + 1) + (y * (xTesselation + 1)));
					}
				}
			}
		}
		

		//now that we have the mesh ready to go lets put it in
		MeshFilter meshFilter = g.GetComponent<MeshFilter>();
		if (!meshFilter)
			meshFilter = g.AddComponent<MeshFilter> ();
		
		//David Lycan - Set Mesh method is now based on the Unity version
		#if UNITY_VERSION_PRE_5_2
			meshFilter.vertices = verts.ToArray();
			meshFilter.triangles = triangles.ToArray();
			meshFilter.uv = uvs.ToArray();
			if (graphicsShaderLevel == 1)
			{
				meshFilter.uv2 = uv2s.ToArray();
			}
			meshFilter.normals = normals.ToArray();
		#else
			meshFilter.sharedMesh.SetVertices(verts);
			meshFilter.sharedMesh.SetTriangles(triangles, 0, true); //recalculate bounds
			meshFilter.sharedMesh.SetUVs(0, uvs);
			if (graphicsShaderLevel == 1)
			{
				meshFilter.sharedMesh.SetUVs(1, uv2s);
			}
			meshFilter.sharedMesh.SetNormals (normals);
		#endif

	}
}
