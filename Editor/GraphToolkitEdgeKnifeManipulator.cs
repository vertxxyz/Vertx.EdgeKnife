#if HAS_GRAPH_TOOLKIT
using Unity.GraphToolkit.Editor;

namespace Vertx.EdgeKnife.Editor
{
	internal sealed class GraphToolkitEdgeKnifeManipulator : EdgeKnifeManipulatorBase
	{
		private readonly GraphView _graphView;

		internal GraphToolkitEdgeKnifeManipulator(GraphView graphView)
		{
			_graphView = graphView;
		}

		// TODO add support for redirects
		protected override bool SupportsRedirects => false;

		protected override void Run(Mode mode, EdgeKnifeElement element)
		{
		}
	}
}
#endif