using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace mitaywalle.UI.Packages.GridImage.Runtime
{
	public enum GridRawImageResize
	{
		None,
		Size,
		ValidPositions,
	}

	[RequireComponent(typeof(CanvasRenderer))]
	public class GridRawImage : MaskableGraphic, ILayoutSelfController, ICanvasRaycastFilter
	{
		static Vector2 UV0 = new Vector2(0, 0);
		static Vector2 UV1 = new Vector2(0, 1);
		static Vector2 UV2 = new Vector2(1, 1);
		static Vector2 UV3 = new Vector2(1, 0);

		public Texture texture;
		[SerializeField] GridRawImageResize _resize = GridRawImageResize.ValidPositions;
		[SerializeField] Vector2 _cellSize = Vector2.one * 100;
		[SerializeField] float _extrude;
		[SerializeField] GridShape _shape = GridShape.Default;

		DrivenRectTransformTracker _tracker;

		// 修复：改为实例变量避免多实例冲突
		private List<Vector2Int> _buffer = new List<Vector2Int>();
		// 添加缓存机制
		private List<Vector2Int> _cachedValidPositions = new List<Vector2Int>();
		private bool _cacheDirty = true;
		private RectInt _cachedValidRect;
		private bool _rectCacheDirty = true;

		Color32 color32;
		RectInt _validRect;

		/// <summary>
		/// Image's texture comes from the UnityEngine.Image.
		/// </summary>
		public override Texture mainTexture
		{
			get
			{
				if (texture == null)
				{
					if (material != null && material.mainTexture != null)
					{
						return material.mainTexture;
					}
					return s_WhiteTexture;
				}

				return texture;
			}
		}

		public void SetShape(GridShape shape)
		{
			_shape = shape;
			_cacheDirty = true;
			_rectCacheDirty = true;
			SetAllDirty();
		}

		// 添加缓存获取有效位置的方法
		private void GetCachedValidPositions()
		{
			if (_cacheDirty)
			{
				_cachedValidPositions.Clear();
				_shape.GetValidPositions(_cachedValidPositions);
				_cacheDirty = false;
			}
		}

		// 添加缓存获取有效矩形的方法
		private RectInt GetCachedValidRect()
		{
			if (_rectCacheDirty)
			{
				_cachedValidRect = _shape.GetValidRect();
				_rectCacheDirty = false;
			}
			return _cachedValidRect;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			color32 = color;

			// 使用缓存的有效位置
			GetCachedValidPositions();
			Vector2 offset = rectTransform.pivot * _cellSize;
			switch (_resize)
			{
				case GridRawImageResize.Size:
					{
						offset *= -_shape.Size;
						break;
					}
				case GridRawImageResize.ValidPositions:
					{
						_validRect = GetCachedValidRect();
						offset *= -_validRect.min;
						break;
					}
				case GridRawImageResize.None:
					{
						offset *= -_shape.Size;
						break;
					}
			}

			if (_cachedValidPositions.Count > 0)
			{
				for (int i = 0; i < _cachedValidPositions.Count; i++)
				{
					Vector2 position = _cachedValidPositions[i] * _cellSize + offset;
					DrawPosition(vh, position, i);
				}
			}
		}

		private void DrawPosition(VertexHelper vh, Vector2 p, int i)
		{
			var corners = new Vector4(p.x - _extrude, p.y - _extrude, p.x + _cellSize.x + _extrude, p.y + _cellSize.y + _extrude);

			vh.AddVert(new Vector3(corners.x, corners.y), color32, UV0);
			vh.AddVert(new Vector3(corners.x, corners.w), color32, UV1);
			vh.AddVert(new Vector3(corners.z, corners.w), color32, UV2);
			vh.AddVert(new Vector3(corners.z, corners.y), color32, UV3);

			i *= 4;

			vh.AddTriangle(i + 0, i + 1, i + 2);
			vh.AddTriangle(i + 2, i + 3, i + 0);
		}

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			Vector2 local;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
				return false;

			// 优化：快速边界检查
			if (local.x < -_extrude || local.y < -_extrude ||
				local.x > _cellSize.x * _shape.Size.x + _extrude ||
				local.y > _cellSize.y * _shape.Size.y + _extrude)
				return false;

			// 计算网格位置
			Vector2Int gridPos = new Vector2Int(
				Mathf.FloorToInt(local.x / _cellSize.x),
				Mathf.FloorToInt(local.y / _cellSize.y)
			);

			// 直接检查是否在有效位置
			return _shape.Contains(gridPos);
		}

		public void SetLayoutHorizontal()
		{
			if (_resize == GridRawImageResize.None)
			{
				_tracker.Clear();
				return;
			}

			_tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDelta);

			switch (_resize)
			{
				case GridRawImageResize.Size:
					{
						rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _shape.Size.x * _cellSize.x);
						break;
					}
				case GridRawImageResize.ValidPositions:
					{
						_validRect = GetCachedValidRect();
						rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (_validRect.size.x + 1) * _cellSize.x);
						break;
					}
			}
		}

		public void SetLayoutVertical()
		{
			if (_resize == GridRawImageResize.None)
			{
				_tracker.Clear();
				return;
			}

			switch (_resize)
			{
				case GridRawImageResize.Size:
					{
						rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _shape.Size.y * _cellSize.y);
						break;
					}
				case GridRawImageResize.ValidPositions:
					{
						_validRect = GetCachedValidRect();
						rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (_validRect.size.y + 1) * _cellSize.y);
						break;
					}
			}
		}

		protected override void OnValidate()
		{
			_cacheDirty = true;
			_rectCacheDirty = true;
			SetAllDirty();
			base.OnValidate();
		}

		// void OnDrawGizmosSelected()
		// {
		// 	Vector2 offset = -rectTransform.pivot * (_cellSize * _shape.Size) - Vector2.one / 2;
		// 	_shape.OnDrawGizmos(transform, Color.green, true, _cellSize, offset);
		// }
	}
}