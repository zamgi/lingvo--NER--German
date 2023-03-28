using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Lingvo.NER.NeuralNetwork.Tensors;

namespace Lingvo.NER.NeuralNetwork.Networks
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ConcurrentList< T >
    {
        private const int MAX_SIZE = 1_024_000;
        private T[] _Array;
        private int _Count;

        public ConcurrentList() => _Array = new T[ MAX_SIZE ];

        public int Count => _Count;
        public T this[ int key ] => _Array[ key ];

        public void Add( T t )
        {
            int n = Interlocked.Increment( ref _Count );
            _Array[ n - 1 ] = t;
        }
        public void Clear()
        {
            Interlocked.Exchange( ref _Count, 0 );
            Interlocked.Exchange( ref _Array, null );
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class ComputeGraphTensor : IDisposable
    {
        private readonly WeightTensorFactory _WeightTensorFactory;
        private readonly ConcurrentList<Action> _BackProp;
        private readonly bool _NeedsBackProp;
        private readonly int _DeviceId;
        private readonly bool _IsSubGraph;

        // Visualization for neural network
        // private Microsoft.Msagl.Drawing.Graph m_opsViz;
        // private HashSet<string> m_setEdges;
        //private Microsoft.Msagl.Drawing.Subgraph m_subGraph = null;
        // private Dictionary<string, Microsoft.Msagl.Drawing.Subgraph> m_name2SubGraph = null;

        private readonly List< WeightTensor > _TensorsBindToCurrentGraph;
        internal ComputeGraphTensor( int deviceId, bool needBack = true, ConcurrentList< Action > backprop = null, bool isSubGraph = false ) 
            : this( new WeightTensorFactory(), deviceId, needBack, backprop, isSubGraph ) { }
        private ComputeGraphTensor( WeightTensorFactory weightFactory, int deviceId, bool needBack, ConcurrentList< Action > backprop, bool isSubGraph )
        {
            _BackProp                  = backprop ?? new ConcurrentList< Action >();
            _WeightTensorFactory       = weightFactory;
            _NeedsBackProp             = needBack;
            _DeviceId                  = deviceId;
            _IsSubGraph                = isSubGraph;
            _TensorsBindToCurrentGraph = new List< WeightTensor >();

            //m_visNeuralNetwork = visNetwork;
            //m_name2SubGraph    = new Dictionary<string, Subgraph>();
            //if ( m_visNeuralNetwork )
            //{
            //    // Initialize parameters for neural network visualization
            //    m_opsViz   = new Microsoft.Msagl.Drawing.Graph();
            //    m_setEdges = new HashSet<string>();
            //}
        }

        public void Dispose()
        {
            // We only dispose root computing graph, For sub graph, we don't do it.
            if ( !_IsSubGraph )
            {
                if ( _BackProp != null )
                {
                    _BackProp.Clear();
                }

                if ( _WeightTensorFactory != null )
                {
                    _WeightTensorFactory.Dispose();
                }
            }
            else
            {
                foreach ( WeightTensor wt in _TensorsBindToCurrentGraph )
                {
                    wt.ReleaseWeight();
                }
            }

            _TensorsBindToCurrentGraph.Clear();
        }

        public WeightTensorFactory GetWeightFactory() => _WeightTensorFactory;
        public ComputeGraphTensor CreateSubGraph( string name )
        {
            var subGraph = new ComputeGraphTensor( _WeightTensorFactory, _DeviceId, _NeedsBackProp, _BackProp, isSubGraph: true );
            //if (m_visNeuralNetwork)
            //{
            //    // Create parameters for neural network visualization
            //    subGraph.m_opsViz = m_opsViz;
            //    subGraph.m_setEdges = m_setEdges;
            //    subGraph.m_name2SubGraph = m_name2SubGraph;
            //    if (!m_name2SubGraph.ContainsKey(name))
            //    {
            //        int index = name.LastIndexOf(".");
            //        subGraph.m_subGraph = new Subgraph(name)
            //        {
            //            LabelText = name.Substring(index + 1)
            //        };

            //        m_name2SubGraph.Add(name, subGraph.m_subGraph);

            //        if (m_subGraph == null)
            //        {
            //            m_opsViz.RootSubgraph.AddSubgraph(subGraph.m_subGraph);
            //        }
            //        else
            //        {
            //            m_subGraph.AddSubgraph(subGraph.m_subGraph);
            //        }
            //    }
            //    else
            //    {
            //        subGraph.m_subGraph = m_name2SubGraph[name];
            //    }
            //}

            return (subGraph);
        }

        public void Backward()
        {
            for ( int i = _BackProp.Count - 1; i >= 0; i-- )
            {
                _BackProp[ i ](); // tick!
            }
            _BackProp.Clear();
        }

        public WeightTensor Sigmoid( WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Sigmoid", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            Ops.Sigmoid( res.TWeight, w.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        w.AddSigmoidGradient( res );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }
        public WeightTensor Rsqrt( WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Rsqrt", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            Ops.Rsqrt( res.TWeight, w.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        using ( var tmp = Ops.Pow( null, res.TWeight, 3.0f ) )
                        {
                            using var tmp2 = Ops.Mul( null, tmp, res.TGradient );
                            using var tmp3 = Ops.Mul( null, tmp2, -0.5f );
                            w.CopyOrAddGradient( tmp3 );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }
        public WeightTensor AddTanh( WeightTensor w1, WeightTensor w2 )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w1.Sizes, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name )}.AddTanh", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2 }, res );

            Ops.AddTanh( res.TWeight, w1.TWeight, w2.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w1.NeedGradient )
                    {
                        w1.AddTanhGradient( res );
                    }

                    if ( w2.NeedGradient )
                    {
                        w2.AddTanhGradient( res );
                    }

                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor AddTanh( WeightTensor w1, WeightTensor w2, WeightTensor w3 )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w1.Sizes, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name, w3.Name )}.AddTanh", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient || w3.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2, w3 }, res );

            Ops.AddTanh3( res.TWeight, w1.TWeight, w2.TWeight, w3.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w1.NeedGradient )
                    {
                        w1.AddTanhGradient( res );
                    }

                    if ( w2.NeedGradient )
                    {
                        w2.AddTanhGradient( res );
                    }

                    if ( w3.NeedGradient )
                    {
                        w3.AddTanhGradient( res );
                    }

                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Mul( WeightTensor w, float v, bool inPlace = false )
        {
            WeightTensor res;
            if ( inPlace )
            {
                res = w.CopyWeightsRef( $"{GetHashString( w.Name )}.MulV", w.NeedGradient );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.MulV", graphToBind: this, needGradient: w.NeedGradient );
            }

            VisualizeNodes( w, res );

            Ops.Mul( res.TWeight, w.TWeight, v );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();

                        if ( inPlace && res.TGradient.IsOwnerExclusive() && w.IsGradientNull() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                            Ops.Mul( w.TGradient, res.TGradient, v );
                        }
                        else
                        {
                            Ops.AddMulV( w.TGradient, w.TGradient, res.TGradient, v );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }


        public WeightTensor Div( WeightTensor w, float v, bool inPlace = false )
        {            
            WeightTensor res;
            if ( inPlace )
            {
                res = w.CopyWeightsRef( $"{GetHashString( w.Name )}.DivV", w.NeedGradient );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.DivV", graphToBind: this, needGradient: w.NeedGradient );
            }

            VisualizeNodes( w, res );

            Ops.Div( res.TWeight, w.TWeight, v );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();

                        if ( inPlace && res.TGradient.IsOwnerExclusive() && w.IsGradientNull() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                            Ops.Div( w.TGradient, res.TGradient, v );
                        }
                        else
                        {
                            Ops.AddMulV( w.TGradient, w.TGradient, res.TGradient, 1.0f / v );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public void Bind( WeightTensor w ) => _TensorsBindToCurrentGraph.Add( w );
        public void Unbind( WeightTensor w ) => _TensorsBindToCurrentGraph.Remove( w );

        /// <summary>
        /// Result = w1 * w2 + w3 * w4
        /// </summary>
        public WeightTensor EltMulMulAdd( WeightTensor w1, WeightTensor w2, WeightTensor w3, WeightTensor w4 )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w1.Sizes, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name, w3.Name, w4.Name )}.EltMulMulAdd", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient || w3.NeedGradient || w4.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2, w3, w4 }, res );

            Ops.MulMulAdd( res.TWeight, w1.TWeight, w2.TWeight, w3.TWeight, w4.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( w1.NeedGradient )
                    {
                        w1.AddMulGradient( w2.TWeight, res.TGradient );
                    }
                    if ( w2.NeedGradient )
                    {
                        w2.AddMulGradient( w1.TWeight, res.TGradient );
                    }
                    if ( w3.NeedGradient )
                    {
                        w3.AddMulGradient( w4.TWeight, res.TGradient );
                    }
                    if ( w4.NeedGradient )
                    {
                        w4.AddMulGradient( w3.TWeight, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                // These tensors' weights will be used during back-propogation, so we unbind them from the computing graph
                w1.UnbindFromComputeGraph();
                w2.UnbindFromComputeGraph();
                w3.UnbindFromComputeGraph();
                w4.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor EltMul( WeightTensor w1, WeightTensor w2 )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w1.Sizes, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name )}.EltMul", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2 }, res );

            Ops.Mul( res.TWeight, w1.TWeight, w2.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( w1.NeedGradient )
                    {
                        w1.AddMulGradient( w2.TWeight, res.TGradient );
                    }
                    if ( w2.NeedGradient )
                    {
                        w2.AddMulGradient( w1.TWeight, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                w1.UnbindFromComputeGraph();
                w2.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Add( WeightTensor w1, WeightTensor w2, bool inPlace = false )
        {
            WeightTensor res;
            if ( inPlace )
            {
                res = w1.CopyWeightsRef( $"{GetHashString( w1.Name )}.Add", needGradient: (w1.NeedGradient || w2.NeedGradient) );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w1.Sizes, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name )}.Add", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient) );
            }

            VisualizeNodes( new WeightTensor[] { w1, w2 }, res );

            Ops.Add( res.TWeight, w1.TWeight, w2.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( w1.NeedGradient )
                    {
                        if ( res.TGradient.IsOwnerExclusive() && w1.IsGradientNull() )
                        {
                            w1.TGradient = res.TGradient.CopyRef();
                        }
                        else
                        {
                            w1.CopyOrAddGradient( res );
                        }
                    }
                    if ( w2.NeedGradient )
                    {
                        if ( res.TGradient.IsOwnerExclusive() && w2.IsGradientNull() )
                        {
                            w2.TGradient = res.TGradient.CopyRef();
                        }
                        else
                        {
                            w2.CopyOrAddGradient( res );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Sum( WeightTensor w, int dim )
        {
            var newSizes = w.Sizes.ToArray();
                newSizes[ dim ] = 1;

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( newSizes, _DeviceId, name: $"{w.Name}.Sum", graphToBind: this, needGradient: w.NeedGradient );
            Ops.Sum( res.TWeight, w.TWeight, dim );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        using var tmp = res.TGradient.Expand( w.Sizes );
                        w.CopyOrAddGradient( tmp );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Log( WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Log", graphToBind: this, needGradient: w.NeedGradient );

            Ops.Log( res.TWeight, w.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        Ops.AddDiv( w.TGradient, w.TGradient, res.TGradient, w.TWeight );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                w.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Add( WeightTensor w, float v )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.AddTV", graphToBind: this, needGradient: w.NeedGradient );

            VisualizeNodes( new WeightTensor[] { w }, res );

            Ops.Add( res.TWeight, w.TWeight, v );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        if ( res.TGradient.IsOwnerExclusive() && w.IsGradientNull() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                        }
                        else
                        {
                            w.CopyOrAddGradient( res );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Sub( float v, WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.SubVT", graphToBind: this, needGradient: w.NeedGradient );

            VisualizeNodes( new WeightTensor[] { w }, res );

            Ops.Sub( res.TWeight, v, w.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        Ops.Sub( w.TGradient, w.TGradient, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Tanh( WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Tanh", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            Ops.Tanh( res.TWeight, w.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        w.AddTanhGradient( res );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Relu( WeightTensor w, bool inPlace = false )
        {
            WeightTensor res;
            if ( inPlace )
            {
                res = w.CopyWeightsRef( $"{GetHashString( w.Name )}.Relu", needGradient: w.NeedGradient );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Relu", graphToBind: this, needGradient: w.NeedGradient );
            }
            VisualizeNodes( w, res );


            Ops.Relu( res.TWeight, w.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        if ( inPlace && w.IsGradientNull() && res.TGradient.IsOwnerExclusive() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                            Ops.ReluD( w.TGradient, w.TWeight, w.TGradient );
                        }
                        else
                        {
                            Ops.AddReluD( w.TGradient, w.TGradient, w.TWeight, res.TGradient );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                w.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor MulBatch( WeightTensor w1, WeightTensor w2, float alpha = 1.0f )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { w1.TWeight.Sizes[ 0 ], w1.TWeight.Sizes[ 1 ], w2.TWeight.Sizes[ 2 ] }, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name )}.MulBatch", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2 }, res );

            Ops.AddmmBatch( res.TWeight, 0.0f, res.TWeight, alpha, w1.TWeight, w2.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( w1.NeedGradient )
                    {
                        using Tensor tW2 = w2.TWeight.Transpose( 1, 2 );
                        Ops.AddmmBatch( w1.TGradient, 1.0f, w1.TGradient, alpha, res.TGradient, tW2 );
                    }
                    if ( w2.NeedGradient )
                    {
                        using Tensor tW1 = w1.TWeight.Transpose( 1, 2 );
                        Ops.AddmmBatch( w2.TGradient, 1.0f, w2.TGradient, alpha, tW1, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                w1.UnbindFromComputeGraph();
                w2.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Mul( WeightTensor w1, WeightTensor w2, float alpha = 1.0f )
        {
            int n = w1.Rows;
            int d = w2.Columns;
            WeightTensor res;

            res = _WeightTensorFactory.CreateWeightTensor( n, d, _DeviceId, name: $"{GetHashString( w1.Name, w2.Name )}.Mul", graphToBind: this, needGradient: (w1.NeedGradient || w2.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { w1, w2 }, res );

            Ops.Addmm( res.TWeight, 0.0f, res.TWeight, alpha, w1.TWeight, w2.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( w1.NeedGradient )
                    {
                        using Tensor tW2 = w2.TWeight.Transpose();
                        Ops.Addmm( w1.TGradient, 1.0f, w1.TGradient, alpha, res.TGradient, tW2 );
                    }
                    if ( w2.NeedGradient )
                    {
                        using Tensor tW1 = w1.TWeight.Transpose();
                        Ops.Addmm( w2.TGradient, 1.0f, w2.TGradient, alpha, tW1, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                w1.UnbindFromComputeGraph();
                w2.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Affine( WeightTensor m1, WeightTensor m2, WeightTensor mbias, float alpha = 1.0f )
        {
            if ( m1 == null ) throw (new ArgumentNullException( $"m1 tensor is null" ));
            if ( m2 == null ) throw (new ArgumentNullException( $"m2 tensor is null" ));
            if ( mbias == null ) throw (new ArgumentNullException( $"mbias tensor is null" ));

            WeightTensor t1 = m1;
            WeightTensor t2 = m2;
            WeightTensor t3 = mbias;

            int n = t1.Rows;
            int d = t2.Columns;
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( n, d, _DeviceId, name: $"{GetHashString( m1.Name, m2.Name, mbias.Name )}.Affine", graphToBind: this, needGradient: (t1.NeedGradient || t2.NeedGradient || t3.NeedGradient) );
            VisualizeNodes( new WeightTensor[] { m1, m2, mbias }, res );

            using ( Tensor t3WExp = t3.TWeight.Expand( n, d ) )
            {
                Ops.Addmm( res.TWeight, 1.0f, t3WExp, alpha, t1.TWeight, t2.TWeight );
            }

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    if ( t3.NeedGradient )
                    {
                        using Tensor t3G = t3.TGradient.Expand( n, d );
                        Ops.Add( t3G, t3G, res.TGradient );
                    }
                    if ( t2.NeedGradient )
                    {
                        using Tensor tW2 = t2.TWeight.Transpose();
                        Ops.Addmm( t1.TGradient, 1.0f, t1.TGradient, alpha, res.TGradient, tW2 );
                    }
                    if ( t1.NeedGradient )
                    {
                        using Tensor tW1 = t1.TWeight.Transpose();
                        Ops.Addmm( t2.TGradient, 1.0f, t2.TGradient, alpha, tW1, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                t1.UnbindFromComputeGraph();
                t2.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Transpose( WeightTensor w, int dim1, int dim2 )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Transpose", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            res.TWeight = w.TWeight.Transpose( dim1, dim2 );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        bool isOwnerExclusive = res.TGradient.IsOwnerExclusive();
                        using Tensor gT = res.TGradient.Transpose( dim1, dim2 );
                        if ( isOwnerExclusive && w.IsGradientNull() )
                        {
                            w.TGradient = gT.CopyRef();
                        }
                        else
                        {
                            w.CopyOrAddGradient( gT, res.Name );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Transpose( WeightTensor w )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Columns, w.Rows, _DeviceId, name: $"{GetHashString( w.Name )}.Transpose", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            res.TWeight = w.TWeight.Transpose();
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        bool isOwnerExclusive = res.TGradient.IsOwnerExclusive();

                        using Tensor gT = res.TGradient.Transpose();
                        if ( isOwnerExclusive && w.IsGradientNull() )
                        {
                            w.TGradient = gT.CopyRef();
                        }
                        else
                        {
                            w.CopyOrAddGradient( gT, res.Name );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Argmax( WeightTensor w, int dim )
        {
            Tensor argMaxT = Ops.Argmax( null, w.TWeight, dim );

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( argMaxT.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Argmax", graphToBind: this, needGradient: w.NeedGradient );
            res.TWeight = argMaxT;

            if ( _NeedsBackProp )
            {
                throw new NotSupportedException( $"Argmax operation doesn't support back propagation." );
            }

            return (res);
        }

        private static double PowerA( double a, double b )
        {
            int tmp = (int) (BitConverter.DoubleToInt64Bits( a ) >> 32);
            int tmp2 = (int) (b * (tmp - 1072632447) + 1072632447);
            return BitConverter.Int64BitsToDouble( ((long) tmp2) << 32 );
        }

        private static double Exp( double x )
        {
            var tmp = (long) (1512775 * x + 1072632447);
            return BitConverter.Int64BitsToDouble( tmp << 32 );
        }

        /// <summary>
        /// Top-P sampling for each row in given tensor
        /// </summary>
        /// <returns>The sampled index</returns>
        public WeightTensor TopPSampleIndice( WeightTensor w, List<List<int>> seqs, float topP = 0.95f, float repeatPenalty = 2.0f, float distancePenalty = 10.0f )
        {
            float[] weights = w.ToWeightArray();
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { w.Rows, 1 }, _DeviceId, name: $"{GetHashString( w.Name )}.Sample", graphToBind: this, needGradient: w.NeedGradient );

            //var locker  = new object();
            var rnd     = new Random( DateTime.Now.Millisecond );
            var indices = new float[ w.Rows ];
            float thresholdValue = 1.0f / (float) (w.Columns * 10000.0);

            var tokenId2OffsetInSeq = new Dictionary<int, int>(); // <tokenId, offsetInSeq>. The last offset of the token in the given sequence
            var tokenId2Cnt         = new Dictionary<int, int>(); // <tokenId, count> The number of token in the given sequences
            var weight2tokenId      = new SortedDictionary<float, int>();

            for ( int i = 0; i < w.Rows; i++ )
            {
                int offset = i * w.Columns;
                List<int> seq = seqs[ i ];

                tokenId2OffsetInSeq.Clear();
                tokenId2Cnt        .Clear();
                for ( int j = 0; j < seq.Count; j++ )
                {
                    var seq_j = seq[ j ];
                    if ( !tokenId2OffsetInSeq.ContainsKey( seq_j ) )
                    {
                        tokenId2OffsetInSeq.Add( seq_j, j );
                        tokenId2Cnt.Add( seq_j, 0 );
                    }
                    else
                    {
                        tokenId2OffsetInSeq[ seq_j ] = j;
                    }
                    tokenId2Cnt[ seq_j ]++;
                }

                if ( topP == 0.0f )
                {
                    var maxWeight = float.MinValue;
                    int maxWeightIndice = -1;

                    for ( int j = 0; j < w.Columns; j++ )
                    {
                        float weight = weights[ offset + j ];
                        if ( Math.Abs( weight ) < thresholdValue )
                        {
                            continue;
                        }

                        //Decay weights if tokens has already been generated before
                        if ( tokenId2OffsetInSeq.TryGetValue( j, out var offsetInSeq ) )
                        {
                            weight = (float) ((weight * (1.0 - Exp( (offsetInSeq + 1 - seq.Count) / distancePenalty ))) / PowerA( repeatPenalty, tokenId2Cnt[ j ] ));
                        }

                        if ( Math.Abs( weight ) < thresholdValue )
                        {
                            continue;
                        }

                        if ( maxWeight < weight )
                        {
                            maxWeight = weight;
                            maxWeightIndice = j;
                        }
                    }

                    indices[ i ] = maxWeightIndice;
                }
                else
                {
                    weight2tokenId.Clear();
                    float adjustedSum = 0.0f;
                    for ( int j = 0; j < w.Columns; j++ )
                    {
                        float weight = weights[ offset + j ];
                        if ( (Math.Abs( weight ) < thresholdValue) || weight2tokenId.ContainsKey( weight ) )
                        {
                            continue;
                        }

                        //Decay weights if tokens has already been generated before
                        if ( tokenId2OffsetInSeq.TryGetValue( j, out var offsetInSeq ) )
                        {
                            weight = (float) ((weight * (1.0 - Exp( (offsetInSeq + 1 - seq.Count) / distancePenalty ))) / PowerA( repeatPenalty, tokenId2Cnt[ j ] ));
                        }

                        if ( (Math.Abs( weight ) < thresholdValue) || weight2tokenId.ContainsKey( weight ) )
                        {
                            continue;
                        }

                        adjustedSum += weight;
                        weight2tokenId.Add( weight, j );
                    }

                    float acc = 0.0f;
                    //float seed = 0.0f;
                    //lock ( locker )
                    //{
                    var seed = (float) rnd.NextDouble() * topP * adjustedSum;
                    //}

                    foreach ( var pair in weight2tokenId.Reverse() )
                    {
                        acc += pair.Key;

                        if ( seed <= acc )
                        {
                            indices[ i ] = pair.Value;
                            break;
                        }
                    }
                }
            }

            res.SetWeightArray( indices );

            if ( _NeedsBackProp )
            {
                throw (new NotSupportedException( $"TopPSampleIndice operation doesn't support back propagation." ));
            }

            return (res);
        }

        public WeightTensor Max( WeightTensor w, int dim )
        {
            Tensor argMaxT = Ops.Max( null, w.TWeight, dim );

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( argMaxT.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Max", graphToBind: this, needGradient: w.NeedGradient );
            res.TWeight = argMaxT;

            if ( _NeedsBackProp )
            {
                throw (new NotSupportedException( $"Argmax operation doesn't support back propagation." ));
            }

            return (res);
        }

        public WeightTensor Softmax( WeightTensor w, bool runGradients = true, bool inPlace = false )
        {
            WeightTensor res;
            if ( inPlace )
            {
                res = w.CopyWeightsRef( $"{GetHashString( w.Name )}.Softmax", needGradient: runGradients && w.NeedGradient );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Softmax", graphToBind: this, needGradient: runGradients && w.NeedGradient );
            }

            VisualizeNodes( w, res );

            Ops.Softmax( res.TWeight, w.TWeight );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( runGradients && w.NeedGradient )
                    {
                        if ( inPlace && w.IsGradientNull() && res.TGradient.IsOwnerExclusive() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                        }
                        w.AddSoftmaxGradient( res, inPlace );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                res.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Peek( WeightTensor w, int dim, int ix, int num = 1 )
        {
            long[] sizes = w.Sizes.ToArray();
                   sizes[ dim ] = num;

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Peek", graphToBind: this, needGradient: w.NeedGradient );
            res.TWeight = w.TWeight.Narrow( dim, ix, num );
            res.TGradient = res.NeedGradient ? w.TGradient.Narrow( dim, ix, num ) : null;

            VisualizeNodes( w, res );

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        private string GetHashString( params string[] inputStrings )
        {
            //if (m_visNeuralNetwork)
            //{
            //    string inputString = string.Join("_", inputStrings);
            //    StringBuilder sb = new StringBuilder();
            //    foreach (byte b in GetHash(inputString))
            //    {
            //        sb.Append(b.ToString("X2"));
            //    }

            //    return sb.ToString();
            //}
            return (string.Empty);
        }

        private void VisualizeNodes( WeightTensor sourceNode, WeightTensor targetNode ) => VisualizeNodes( new WeightTensor[] { sourceNode }, targetNode );
        private void VisualizeNodes( IEnumerable<WeightTensor> sourceNodes, WeightTensor targetNode )
        {
            //if (!m_visNeuralNetwork || m_deviceId != 0)
            //{
            //    return;
            //}

            //// Create node for target tensor
            //int index = targetNode.Name.LastIndexOf('.');
            //Microsoft.Msagl.Drawing.Node tgtNode = m_opsViz.AddNode(targetNode.Name);
            //tgtNode.LabelText = targetNode.Name.Substring(index + 1);

            //if (targetNode.IsTrainable)
            //{
            //    tgtNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightSteelBlue;
            //}

            //if (m_subGraph != null)
            //{
            //    // Current compute graph is a sub-graph
            //    m_subGraph.AddNode(tgtNode);
            //}

            //// Create edges for each source node and target node
            //foreach (WeightTensor sourceNode in sourceNodes)
            //{
            //    if (!sourceNode.Name.IsNullOrEmpty() && !targetNode.Name.IsNullOrEmpty())
            //    {
            //        string key = $"{sourceNode.Name}->{targetNode.Name}";
            //        if (m_setEdges.Contains(key))
            //        {
            //            continue;
            //        }

            //        int srcIndex = sourceNode.Name.LastIndexOf('.');
            //        Microsoft.Msagl.Drawing.Node srcNode = m_opsViz.AddNode(sourceNode.Name);
            //        srcNode.LabelText = sourceNode.Name.Substring(srcIndex + 1);
            //        if (sourceNode.IsTrainable)
            //        {
            //            srcNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightSteelBlue;

            //            if (m_subGraph != null)
            //            {
            //                m_subGraph.AddNode(srcNode);
            //            }
            //        }

            //        Edge edge = m_opsViz.AddEdge(sourceNode.Name, targetNode.Name);

            //        m_setEdges.Add(key);
            //    }
            //}
        }
        public void VisualizeNeuralNetToFile( string neuralNetPicFilePath )
        {
            //FastIncrementalLayoutSettings fastSettings = new FastIncrementalLayoutSettings
            //{
            //    AvoidOverlaps = true,
            //    NodeSeparation = 30,
            //    RouteEdges = true
            //};

            //SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings
            //{
            //    FallbackLayoutSettings = fastSettings
            //};

            //m_opsViz.LayoutAlgorithmSettings = settings;

            //Microsoft.Msagl.GraphViewerGdi.GraphRenderer renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(m_opsViz);
            //renderer.CalculateLayout();

            //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)m_opsViz.Width, (int)m_opsViz.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            //renderer.Render(bitmap);

            //bitmap.Save(neuralNetPicFilePath);

            //bitmap.Dispose();
        }

        public WeightTensor IndexSelect( WeightTensor src, float[] idxs )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { idxs.Length, src.Sizes[ ^1 ] }, _DeviceId, name: $"{GetHashString( src.Name )}.IndexSelect", graphToBind: this, needGradient: src.NeedGradient );

            Tensor indice = new Tensor( src.Allocator, DType.Float32, idxs.Length );
            indice.CopyFrom( idxs );
            Ops.IndexSelect( res.TWeight, src.TWeight, indice );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( src.NeedGradient )
                    {
                        res.ReleaseWeight();
                        Ops.IndexSelectGrad( src.TGradient, res.TGradient, indice );
                    }
                    res.Dispose();
                    indice.Dispose();
                };
                _BackProp.Add( backward );
            }
            else
            {
                indice.Dispose();
            }

            return (res);
        }

        public WeightTensor Concate( int dim, params WeightTensor[] wl ) => Concate( wl.ToList(), dim );
        public WeightTensor Concate( List<WeightTensor> wl, int dim )
        {
            if ( wl.Count == 1 )
            {
                return wl[ 0 ];
            }

            var wlNameList = new List<string>();
            var twl = new List<Tensor>();
            long sumDimSize = 0;
            var needGradient = false;

            foreach ( WeightTensor item in wl )
            {
                WeightTensor m = item;
                sumDimSize += m.Sizes[ dim ];

                twl.Add( m.TWeight );
                wlNameList.Add( item.Name );

                needGradient = (needGradient || m.NeedGradient);
            }

            var newSizes = new long[ wl[ 0 ].Sizes.Length ];
            for ( int i = 0; i < newSizes.Length; i++ )
            {
                newSizes[ i ] = wl[ 0 ].Sizes[ i ];
            }
            newSizes[ dim ] = sumDimSize;

            var wlName = string.Join( "_", wlNameList );
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( newSizes, _DeviceId, name: $"{GetHashString( wlName )}.Concat", graphToBind: this, needGradient: needGradient );
            VisualizeNodes( wl, res );

            Ops.Concat( res.TWeight, dim, twl.ToArray() );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    res.ReleaseWeight();
                    var isOwnerExclusive = res.TGradient.IsOwnerExclusive();

                    long sx = 0;
                    foreach ( WeightTensor item in wl )
                    {
                        WeightTensor m = item;
                        if ( item.NeedGradient )
                        {
                            using Tensor tTmp = res.TGradient.Narrow( dim, sx, m.Sizes[ dim ] );
                            if ( isOwnerExclusive && m.IsGradientNull() )
                            {
                                m.TGradient = tTmp.CopyRef();
                            }
                            else
                            {
                                m.CopyOrAddGradient( tTmp, res.Name );
                            }
                        }

                        sx += m.Sizes[ dim ];
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor TransposeBatch( WeightTensor w, int batchSize )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.TransposeBatch", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            int sizeEveryBatch = w.Rows / batchSize;
            using ( Tensor tWView = w.TWeight.View( sizeEveryBatch, batchSize, w.Columns ) )
            {
                using Tensor tWViewPermute = tWView.Permute( 1, 0, 2 );
                using Tensor tW2 = Ops.AsContiguous( tWViewPermute );
                res.TWeight = tW2.View( w.Rows, w.Columns );
            }

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        using Tensor g = w.TGradient.View( sizeEveryBatch, batchSize, w.Columns );
                        using Tensor t2 = res.TGradient.View( batchSize, sizeEveryBatch, w.Columns );
                        using Tensor t2Permute = t2.Permute( 1, 0, 2 );
                        Ops.Add( g, g, t2Permute );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public List<WeightTensor> SplitColumns2( WeightTensor w, params int[] sizes )
        {
            var resList = new List<WeightTensor>();

            int x = 0;
            foreach ( int size in sizes )
            {
                WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Rows, size, _DeviceId, name: $"{GetHashString( w.Name )}.SplitColumn", graphToBind: this, needGradient: w.NeedGradient );
                VisualizeNodes( w, res );

                res.TWeight = w.TWeight.Narrow( 1, x, size );
                resList.Add( res );

                x += size;
            }

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        x = 0;
                        int i = 0;
                        foreach ( WeightTensor item in resList )
                        {
                            WeightTensor item_i = item;
                            using ( Tensor mG = w.TGradient.Narrow( 1, x, sizes[ i ] ) )
                            {
                                Ops.Add( mG, mG, item_i.TGradient );
                            }

                            item.Dispose();

                            x += sizes[ i ];
                            i++;
                        }
                    }
                    else
                    {
                        foreach ( WeightTensor item in resList )
                        {
                            item.Dispose();
                        }
                    }
                };
                _BackProp.Add( backward );
            }

            return (resList);
        }

        public WeightTensor AsContiguous( WeightTensor w, bool shareTensor = true )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.AsContiguous", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            res.TWeight = Ops.AsContiguous( w.TWeight );

            if ( shareTensor )
            {
                w.ReleaseWeight();
                w.TWeight = res.TWeight.CopyRef();
            }

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        if ( res.TGradient.IsOwnerExclusive() && w.IsGradientNull() )
                        {
                            w.TGradient = res.TGradient.CopyRef();
                        }
                        else
                        {
                            w.CopyOrAddGradient( res );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor View( WeightTensor w, params long[] dims )
        {
            var hasNegOne = false;
            int negOneIdx = 0;
            long totalGivenSize = 1;
            for ( int i = 0; i < dims.Length; i++ )
            {
                long dim = dims[ i ];
                if ( dim == -1 )
                {
                    if ( hasNegOne )
                    {
                        throw (new ArgumentException( $"View operation only allows single -1 in dims." ));
                    }

                    hasNegOne = true;
                    negOneIdx = i;
                }
                else
                {
                    totalGivenSize *= dim;
                }
            }

            if ( hasNegOne )
            {
                long totalSrcSize = 1;
                foreach ( int size in w.Sizes )
                {
                    totalSrcSize *= size;
                }

                dims[ negOneIdx ] = totalSrcSize / totalGivenSize;
            }


            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( dims, _DeviceId, name: $"{w.Name}.View", graphToBind: this, needGradient: w.NeedGradient );
            //  VisualizeNodes(w, res);

            res.TWeight = w.TWeight.View( dims );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();
                        bool isOwnerExclusive = res.TGradient.IsOwnerExclusive();

                        using Tensor resGConti = Ops.AsContiguous( res.TGradient );
                        using Tensor resG = resGConti.View( w.Sizes );
                        if ( isOwnerExclusive && w.IsGradientNull() )
                        {
                            w.TGradient = resG.CopyRef();
                        }
                        else
                        {
                            w.CopyOrAddGradient( resG, res.Name );
                        }
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Scatter( WeightTensor source, WeightTensor indices, int dim, params long[] shape )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( shape, _DeviceId, name: $"{GetHashString( source.Name + indices.Name )}.Scatter", graphToBind: this, needGradient: source.NeedGradient );

            Ops.Fill( res.TWeight, 0.0f );
            Ops.Scatter( res.TWeight, source.TWeight, dim, indices.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( source.NeedGradient )
                    {
                        res.ReleaseWeight();
                        using var tmp = Ops.Gather( null, res.TGradient, dim, indices.TWeight );
                        source.CopyOrAddGradient( tmp );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor ScatterAdd( WeightTensor source, WeightTensor indices, int dim, params long[] shape )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( shape, _DeviceId, name: $"{GetHashString( source.Name + indices.Name )}.Scatter", graphToBind: this, needGradient: source.NeedGradient );

            Ops.Fill( res.TWeight, 0.0f );
            Ops.ScatterAdd( res.TWeight, source.TWeight, dim, indices.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( source.NeedGradient )
                    {
                        res.ReleaseWeight();
                        using var tmp = Ops.Gather( null, res.TGradient, dim, indices.TWeight );
                        source.CopyOrAddGradient( tmp );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Scatter( WeightTensor indices, float val, int dim, bool needGradient = true, params long[] shape )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( shape, _DeviceId, name: $"{GetHashString( indices.Name )}.Scatter", graphToBind: this, needGradient: needGradient );

            Ops.Fill( res.TWeight, 0.0f );
            Ops.ScatterFill( res.TWeight, val, dim, indices.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( needGradient )
                    {
                        res.ReleaseWeight();
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Expand( WeightTensor w, params long[] dims )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( dims, _DeviceId, name: $"{GetHashString( w.Name )}.Expand", graphToBind: this, needGradient: w.NeedGradient );
            VisualizeNodes( w, res );

            res.TWeight = w.TWeight.Expand( dims );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( w.NeedGradient )
                    {
                        res.ReleaseWeight();

                        using var tmpMGrad = w.TGradient.Expand( dims ); // expand input tensor at first
                        Ops.AtomicAdd( tmpMGrad, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public (WeightTensor r1, WeightTensor r2) SplitColumns( WeightTensor w, int size1, int size2 )
        {
            List<WeightTensor> res = SplitColumns2( w, size1, size2 );
            return (res[ 0 ], res[ 1 ]);
        }

        public (WeightTensor r1, WeightTensor r2, WeightTensor r3) SplitColumns( WeightTensor w, int size1, int size2, int size3 )
        {
            List<WeightTensor> res = SplitColumns2( w, size1, size2, size3 );
            return (res[ 0 ], res[ 1 ], res[ 2 ]);
        }

        private Tensor BuildRandomTensor( int rows, int columns, int batchSize, float prob )
        {
            using Tensor noise = new Tensor( TensorAllocator.Allocator( _DeviceId ), DType.Float32, rows / batchSize, columns );
            float[] w = Tensors.RandomGenerator.BuildRandomBernoulliWeight( new long[] { rows / batchSize, columns }, prob );
            noise.SetElementsAsFloat( w );

            if ( (rows / batchSize) == 1 )
            {
                return noise.Expand( rows, columns );
            }
            else
            {
                return noise.RepeatTensor( batchSize, 1 );
            }
        }

        public WeightTensor LayerNorm( WeightTensor src, WeightTensor alpha, WeightTensor beta, float eps = 1e-9f )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( src.Sizes, _DeviceId, name: $"{GetHashString( src.Name, alpha.Name, beta.Name )}.LayerNorm", graphToBind: this, needGradient: src.NeedGradient );
            VisualizeNodes( new WeightTensor[] { src, alpha, beta }, res );

            Ops.LayerNorm( res.TWeight, src.TWeight, alpha.TWeight, beta.TWeight, eps );
            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( src.NeedGradient )
                    {
                        Ops.LayerNormGrad( src.TGradient, alpha.TGradient, beta.TGradient, res.TGradient, res.TWeight, src.TWeight, alpha.TWeight, beta.TWeight, eps );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );

                src.UnbindFromComputeGraph();
                alpha.UnbindFromComputeGraph();
                beta.UnbindFromComputeGraph();
            }

            return (res);
        }

        public WeightTensor Dropout( WeightTensor w, int batchSize, float drop_prob, bool inPlace = false )
        {
            if ( drop_prob == 0 || !_NeedsBackProp )
            {
                return w;
            }

            // Generate noise tensor
            float p = 1.0f - drop_prob;
            Tensor noise = BuildRandomTensor( w.Rows, w.Columns, batchSize, p );

            WeightTensor res;
            if ( inPlace )
            {
                res = w.CopyWeightsRef( $"{GetHashString( w.Name )}.Dropout", needGradient: w.NeedGradient );
            }
            else
            {
                res = _WeightTensorFactory.CreateWeightTensor( w.Sizes, _DeviceId, name: $"{GetHashString( w.Name )}.Dropout", graphToBind: this, needGradient: w.NeedGradient );
            }
            VisualizeNodes( w, res );

            Ops.Mul( res.TWeight, w.TWeight, noise );

            void backward()
            {
                if ( w.NeedGradient )
                {
                    res.ReleaseWeight();

                    if ( inPlace && w.IsGradientNull() && res.TGradient.IsOwnerExclusive() )
                    {
                        w.TGradient = res.TGradient.CopyRef();
                    }

                    w.AddMulGradient( noise, res.TGradient, inPlace );
                }
                res.Dispose();
                noise.Dispose();
            };
            _BackProp.Add( backward );

            return (res);
        }

        public WeightTensor Gather( WeightTensor src, WeightTensor indices, int dim )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( indices.Sizes, _DeviceId, name: $"Gather_{_DeviceId}", graphToBind: this, needGradient: src.NeedGradient );
            Ops.Gather( res.TWeight, src.TWeight, dim, indices.TWeight );

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( src.NeedGradient )
                    {
                        res.ReleaseWeight();
                        Ops.ScatterAdd( src.TGradient, res.TGradient, dim, indices.TWeight );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor Select( WeightTensor src, int dim, int index )
        {
            var resTWeight = src.TWeight.Select( dim, index );

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( resTWeight.Sizes, _DeviceId, name: $"Select_{_DeviceId}", graphToBind: this, needGradient: src.NeedGradient );
            res.TWeight = resTWeight;

            if ( _NeedsBackProp )
            {
                void backward()
                {
                    if ( src.NeedGradient )
                    {
                        res.ReleaseWeight();
                        using var tmpG = src.TGradient.Select( dim, index );
                        Ops.Add( tmpG, tmpG, res.TGradient );
                    }
                    res.Dispose();
                };
                _BackProp.Add( backward );
            }

            return (res);
        }

        /// <returns>Shape: [batch_size, seq_len]</returns>
        public WeightTensor LeftShiftTokens( List<List<int>> input, int lastTokenToPad )
        {
            var buf = new float[ input.Count * input[ 0 ].Count ];
            for ( int i = 0; i < input.Count; i++ )
            {
                var input_i = input[ i ];
                var offset = i * input_i.Count;
                for ( int j = 0; j < input_i.Count - 1; j++ )
                {
                    buf[ offset + j ] = input_i[ j + 1 ];
                }
                buf[ (i + 1) * input_i.Count - 1 ] = lastTokenToPad;
            }

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( input.Count, input[ 0 ].Count, _DeviceId, name: $"LeftShiftTokens_{_DeviceId}", graphToBind: this, needGradient: false );
            res.SetWeightArray( buf );

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor CreateTokensTensor( List<List<int>> input )
        {
            var buf = new float[ input.Count * input[ 0 ].Count ];
            for ( int i = 0; i < input.Count; i++ )
            {
                var input_i = input[ i ];
                var offset = i * input_i.Count;
                for ( int j = 0; j < input_i.Count; j++ )
                {
                    buf[ offset + j ] = input_i[ j ];
                }
            }

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( input.Count, input[ 0 ].Count, _DeviceId, name: $"TokensTensor_{_DeviceId}", graphToBind: this, needGradient: false );
            res.SetWeightArray( buf );

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        /// <returns>shape: (batch_size, sequence_padded_length, dim)</returns>
        public WeightTensor BuildFeatureMask( int paddedLength, List<int> appliedLengths, int dim )
        {
            var buf = new float[ appliedLengths.Count * paddedLength * dim ];
            //Array.Fill( buf, 0.0f );
            for ( int k = 0; k < appliedLengths.Count; k++ )
            {
                var appliedLengths_k = appliedLengths[ k ];
                var offset = k * (paddedLength * dim);
                for ( int i = 0; i < appliedLengths_k; i++ )
                {
                    Array.Fill( buf, 1.0f, offset + i * dim, dim );
                }
            }

            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { appliedLengths.Count, paddedLength, dim }, _DeviceId, name: $"FeatureMask_{_DeviceId}", graphToBind: this, needGradient: false );
            res.SetWeightArray( buf );

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor BuildPadSelfMask( int paddedLength, float[] originalLengths )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { originalLengths.Length, paddedLength, paddedLength }, _DeviceId, name: $"SelfMask_{_DeviceId}", graphToBind: this, needGradient: false );
            using ( Tensor originalLengthsTensor = new Tensor( res.Allocator, DType.Float32, originalLengths.Length ) )
            {
                originalLengthsTensor.CopyFrom( originalLengths );
                Ops.BuildSelfMask( res.TWeight, originalLengthsTensor, paddedLength, 0.0f, -99999999.0f );
            }

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor BuildSelfTriMask( int paddedLength, float[] originalLengths )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { originalLengths.Length, paddedLength, paddedLength }, _DeviceId, name: $"SelfTriMask_{_DeviceId}", graphToBind: this, needGradient: false );
            using ( Tensor originalLengthsTensor = new Tensor( res.Allocator, DType.Float32, originalLengths.Length ) )
            {
                originalLengthsTensor.CopyFrom( originalLengths );
                Ops.BuildSelfTriMask( res.TWeight, originalLengthsTensor, paddedLength, 0.0f, -99999999.0f );
            }

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor BuildTriMask( int paddedLength, int batchSize )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { paddedLength, paddedLength }, _DeviceId, name: $"SelfTriMask2_{_DeviceId}", graphToBind: this, needGradient: false );
            Ops.BuildTriMask( res.TWeight, 0.0f, -99999999.0f );

            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public WeightTensor BuildSrcTgtMask( int srcPaddedLength, int tgtPaddedLength, float[] tgtOriginalLengths, float[] srcOriginalLengths )
        {
            WeightTensor res = _WeightTensorFactory.CreateWeightTensor( new long[] { tgtOriginalLengths.Length, tgtPaddedLength, srcPaddedLength }, _DeviceId, name: $"SrcTgtMask_{_DeviceId}", graphToBind: this, needGradient: false );

            using ( Tensor tgtOriginalLengthsTensor = new Tensor( res.Allocator, DType.Float32, tgtOriginalLengths.Length ) )            
            using ( Tensor srcOriginalLengthsTensor = new Tensor( res.Allocator, DType.Float32, srcOriginalLengths.Length ) )
            {
                srcOriginalLengthsTensor.CopyFrom( srcOriginalLengths );
                tgtOriginalLengthsTensor.CopyFrom( tgtOriginalLengths );
                Ops.BuildSrcTgtMask( res.TWeight, srcOriginalLengthsTensor, tgtOriginalLengthsTensor, srcPaddedLength, tgtPaddedLength, 0.0f, -99999999.0f );
            }
            
            if ( _NeedsBackProp )
            {
                void backward() => res.Dispose();
                _BackProp.Add( backward );
            }

            return (res);
        }

        public float CrossEntropyLoss( WeightTensor probs, WeightTensor truthTgtSeqs, float graident = 1.0f, float smooth = 0.0f )
        {
            var scatterIdxTensor = View( truthTgtSeqs, new long[] { -1, 1 } );
            var loss             = Gather( probs, scatterIdxTensor, 1 );

            if ( 0.0f < smooth )
            {
                loss = Add( loss, smooth );
            }

            loss = Log( loss );
            loss = Mul( loss, -1.0f );
            loss.FillGradient( graident );

            return (loss.ToWeightArray().Sum() / loss.ElementCount);
        }
    }
}
