using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectEachMaterial : MonoBehaviour
{

	static int baseColorId = Shader.PropertyToID("_BaseColor");
	static MaterialPropertyBlock block;
	[SerializeField]
	Color baseColor = Color.white;
	void OnValidate()
	{
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		block.SetColor(baseColorId, baseColor);
		GetComponent<Renderer>().SetPropertyBlock(block);
	}
	void Awake()
	{
		OnValidate();
	}
}
