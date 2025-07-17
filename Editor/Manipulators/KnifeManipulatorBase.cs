#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.EdgeKnife.Editor
{
	/// <summary>
	/// A <see cref="Manipulator"/> for graphs that adds a path-drawing edge manipulation tool.
	/// Ctrl+Right-Click drag will delete edges, Shift+Right-Click drag creates an edge redirect node.
	/// </summary>
	public abstract class KnifeManipulatorBase : Manipulator
	{
		protected enum Mode
		{
			Inactive,
			Additive,
			Subtractive
		}

		// The knife is active if the pointer has been captured and the mode is not inactive.
		private Mode _mode = Mode.Inactive;
		private int _targetPointerId;
		private readonly EdgeKnifeElement _element = new();
		
		protected abstract bool SupportsAdditive { get; }

		protected KnifeManipulatorBase()
		{
			_element.RegisterCallback<PointerDownEvent>(OnPointerDown);
			_element.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			_element.RegisterCallback<PointerUpEvent>(OnPointerUp);
		}

		/// <inheritdoc />
		protected override void RegisterCallbacksOnTarget()
		{
			target.hierarchy.Add(_element);
			_element.StretchToParentSize();
			target.RegisterCallback<KeyDownEvent>(OnKeyDown);
			target.RegisterCallback<PointerDownEvent>(OnPointerDown);
			target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
		}

		/// <inheritdoc />
		protected override void UnregisterCallbacksFromTarget()
		{
			_element.RemoveFromHierarchy();
			target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
			target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Escape)
				CancelInteraction();
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			// Cancel any prior interactions because we clicked.
			CancelInteraction();

			if (evt.button != 1)
				return;

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (evt.modifiers)
			{
				case EventModifiers.Shift:
					if (!SupportsAdditive)
					{
						// No logic if callback isn't provided.
						return;
					}
					
					_mode = Mode.Additive;
					_element.Color = Color.white;
					break;
				case EventModifiers.Control:
					_mode = Mode.Subtractive;
					_element.Color = new Color(1, 0.5f, 0);
					break;
				default:
					return;
			}

			_targetPointerId = evt.pointerId;
			_element.RecordPoint(evt.position);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (_mode == Mode.Inactive || _targetPointerId != evt.pointerId)
				return;

			if (!_element.HasPointerCapture(evt.pointerId))
			{
				_element.CapturePointer(evt.pointerId);
				_targetPointerId = evt.pointerId;
			}

			_element.RecordPoint(evt.position);
			evt.StopImmediatePropagation();
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (_mode == Mode.Inactive || _targetPointerId != evt.pointerId)
			{
				CancelInteraction();
				return;
			}

			_element.RecordPoint(evt.position);
			Run(_mode, _element);
			CancelInteraction();
			evt.StopImmediatePropagation();
		}

		protected abstract void Run(Mode mode, EdgeKnifeElement element);

		private void CancelInteraction()
		{
			if (_mode == Mode.Inactive || !_element.HasPointerCapture(_targetPointerId))
				return;

			_element.ReleasePointer(_targetPointerId);
			_mode = Mode.Inactive;
			_element.Reset();
		}

		protected sealed class EdgeKnifeElement : VisualElement
		{
			public Color Color { get; set; }
			public IReadOnlyList<Vector2> Points => _points;

			private readonly List<Vector2> _points = new();

			public void Reset()
			{
				_points.Clear();
				MarkDirtyRepaint();
			}

			public EdgeKnifeElement()
			{
				pickingMode = PickingMode.Ignore;
				generateVisualContent += GenerateVisualContent;
			}

			private void GenerateVisualContent(MeshGenerationContext obj)
			{
				Painter2D painter = obj.painter2D;
				painter.lineWidth = 1;
				painter.strokeColor = Color;
				if (_points.Count == 0)
					return;

				painter.BeginPath();
				painter.MoveTo(_points[0]);
				for (var i = 1; i < _points.Count; i++)
				{
					Vector2 point = _points[i];
					painter.LineTo(point);
				}

				painter.Stroke();
			}

			public void RecordPoint(Vector2 worldPoint)
			{
				const int minimumPointDistance = 8;

				Vector2 localPoint = this.WorldToLocal(worldPoint);

				if (_points.Count != 0 && (_points[^1] - localPoint).sqrMagnitude < minimumPointDistance)
					return;

				_points.Add(localPoint);
				MarkDirtyRepaint();
			}

			public static bool TryGetSegmentIntersection(
				Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
				out Vector2 intersection
			)
			{
				// Get the segments' parameters.
				float dx12 = p2.x - p1.x;
				float dy12 = p2.y - p1.y;
				float dx34 = p4.x - p3.x;
				float dy34 = p4.y - p3.y;

				// Solve for t1 and t2
				float denominator = dy12 * dx34 - dx12 * dy34;

				float t1 =
					((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
					/ denominator;

				if (float.IsInfinity(t1))
				{
					// The lines are parallel (or close enough to it).
					intersection = default;
					return false;
				}

				float t2 =
					((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
					/ -denominator;

				// The segments intersect if t1 and t2 are between 0 and 1.
				if (!(t1 is > 0 and < 1 &&
				      t2 is > 0 and < 1))
				{
					intersection = default;
					return false;
				}

				// Find the point of intersection.
				intersection = new Vector2(
					t1 * dx12 + p1.x,
					t1 * dy12 + p1.y
				);
				return true;
			}
		}
	}
}