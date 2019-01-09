// using System;
// using Entia.Core;
// using Entia.Messages;
// using Entia.Modules;
// using Entia.Modules.Query;
// using Entia.Queryables;

// namespace Entia.Messages
// {
//     // public struct OnSegment : IMessage
//     // {
//     //     public Components2.Segment Segment;
//     // }
// }

// namespace Entia.Modules
// {
//     // public sealed class Write2<T> where T : struct, IComponent
//     // {
//     //     public bool TryBuild(Components2.Segment segment, int index, out Write<T> item)
//     //     {
//     //         if (segment.TryBuffer<T>(out var buffer) && buffer.TryGet(index, out var chunk, out var adjusted))
//     //         {
//     //             item = new Write<T>(chunk, adjusted);
//     //             return true;
//     //         }

//     //         item = default;
//     //         return false;
//     //     }
//     // }

//     // public sealed class Write3<T> where T : struct, IComponent
//     // {
//     //     public bool TryBuild(in Components2.Segment.Chunk chunk, int offset, int index, out Write<T> item)
//     //     {
//     //         var local = IndexUtility<IComponent>.Cache<T>.Index.local - offset;
//     //         if (chunk.Stores[offset] is T[] store)
//     //         {
//     //             item = new Write<T>(store, index);
//     //             return true;
//     //         }

//     //         item = default;
//     //         return false;
//     //     }
//     // }

//     /*
//     All<Entity, Read<Position>, Write<Velocity>>
//     (Entity[] entities, Position[] positions, Velocity[] velocities)

//     All<Entity, Maybe<Read<Position>>, Write<Velocity>>
//     (Entity[] entities, (bool has, Position[] positions), Velocity[] velocities)

//     All<Entity, Any<Read<Position>, Write<Velocity>>>
//     (Entity[] entities, (byte index, (int index, Position[] positions), (int index, Velocity[] velocities)))
//     */

//     public sealed class Group2<T> where T : struct, IQueryable
//     {
//         public ref struct Enumerator
//         {
//             enum States : byte
//             {
//                 GetSegment, MainChunk, OverflowChunks, Done
//             }

//             // TODO: build current item
//             public T Current => default;

//             Group2<T> _group;
//             Components2.Segment _segment;
//             Components2.Chunk _chunk;
//             int _segmentIndex;
//             int _chunkIndex;
//             int _entityIndex;
//             States _state;

//             public Enumerator(Group2<T> group)
//             {
//                 _group = group;
//                 _segment = null;
//                 _chunk = Components2.Chunk.Empty;
//                 _segmentIndex = -1;
//                 _chunkIndex = -1;
//                 _entityIndex = -1;
//                 _state = States.GetSegment;
//             }

//             public bool MoveNext()
//             {
//                 while (true)
//                 {
//                     // TODO: fix this...
//                     switch (_state)
//                     {
//                         case States.GetSegment:
//                             if (++_segmentIndex < _group._segments.count)
//                             {
//                                 _segment = _group._segments.items[_segmentIndex];
//                                 _state = States.MainChunk;
//                             }
//                             else _state = States.Done;
//                             break;
//                         case States.MainChunk:
//                             if (++_entityIndex < _segment.Chunk.Items.Length)
//                             {

//                             }
//                             else
//                             {
//                                 _state = States.OverflowChunks;
//                             }
//                             break;
//                         case States.OverflowChunks:
//                             break;
//                         default: return false;
//                     }

//                     if (_segment == null)
//                     {
//                         if (++_segmentIndex < _group._segments.count) _segment = _group._segments.items[_segmentIndex];
//                         else break;
//                     }

//                     while (++_entityIndex < _segment.Maximum)
//                         if (_segment.GetEntity(_entityIndex).Identifier != 0uL) return true;

//                     _segment = null;
//                     _entityIndex = -1;
//                 }

//                 return false;
//             }

//             public void Reset()
//             {
//                 _segment = null;
//                 _segmentIndex = -1;
//                 _entityIndex = -1;
//             }

//             public void Dispose()
//             {
//                 _group = null;
//                 _segment = null;
//             }
//         }

//         public Query<T> Query;

//         readonly Messages _messages;
//         (Components2.Segment[] items, int count) _segments = (new Components2.Segment[4], 0);

//         public Group2(Messages messages)
//         {
//             _messages = messages;
//             // _messages.React((in OnSegment message) => TryAdd(message.Segment));
//         }

//         public bool TryAdd(Components2.Segment segment)
//         {
//             if (Array.IndexOf(_segments.items, segment) < 0 && Query.Fits(segment.Mask))
//             {
//                 _segments.Push(segment);
//                 return true;
//             }

//             return false;
//         }
//     }
// }