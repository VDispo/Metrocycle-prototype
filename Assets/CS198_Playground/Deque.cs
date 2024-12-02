#nullable enable // to suppress Unity warning message

// modified FROM: https://raw.githubusercontent.com/unageek/Deque/main/src/Deque.cs
// TODO: use package manager instead of vendoring this file

using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

// Array + two unmasked indices approach explained in the following page is used.
//   https://www.snellman.net/blog/archive/2016-12-13-ring-buffers/

public sealed class Deque<T>
{
    private const int DefaultCapacity = 4;

    private T[] _buf = Array.Empty<T>();
    private int _read;
    private int _version;
    private int _write;

    public Deque()
    {
    }

    public Deque(int capacity)
    {
        EnsureCapacity(capacity);
    }

    public int Capacity => _buf.Length;

    public int Count => _write - _read;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new IndexOutOfRangeException();

            return _buf[WrapIndex(_read + index)];
        }
        set
        {
            if ((uint)index >= (uint)Count)
                throw new IndexOutOfRangeException();

            _buf[WrapIndex(_read + index)] = value;
            _version++;
        }
    }

    public bool IsEmpty => _read == _write;

    private bool IsFull => Count == Capacity;

    public void Add(T item)
    {
        AddLast(item);
    }

    public void AddFirst(T item)
    {
        if (IsFull) Grow();

        _read--;
        this[0] = item;
    }

    public void AddLast(T item)
    {
        if (IsFull) Grow();

        _write++;
        this[Count - 1] = item;
    }

    public void Clear()
    {
        RemoveRange(0, Count);
    }

    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(T[] array)
    {
        CopyTo(array, 0);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(0, array, arrayIndex, Count);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (count < 0 || index + count > Count || arrayIndex + count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        var read = WrapIndex(_read);

        if (IsContiguous(index, count))
        {
            Array.Copy(_buf, WrapIndex(read + index), array, arrayIndex, count);
        }
        else
        {
            var leftLen = Capacity - read;
            var leftCount = leftLen - index;
            var rightCount = count - leftCount;

            Array.Copy(_buf, read + index, array, arrayIndex, leftCount);
            Array.Copy(_buf, 0, array, arrayIndex + leftCount, rightCount);
        }
    }

    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (capacity <= Capacity)
            return Capacity;

        try
        {
            Relocate(NextCapacity(capacity));
        }
        catch (OverflowException)
        {
            throw new OutOfMemoryException();
        }

        _version++;
        return Capacity;
    }

    public Deque<T> GetRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || count > Count - index)
            throw new ArgumentOutOfRangeException(nameof(count));

        var deque = new Deque<T>(count);
        CopyTo(index, deque._buf, 0, count);
        deque._write = count;
        return deque;
    }

    public int IndexOf(T item)
    {
        return IndexOf(item, 0);
    }

    public int IndexOf(T item, int index)
    {
        return IndexOf(item, index, Count - index);
    }

    public int IndexOf(T item, int index, int count)
    {
        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || count > Count - index)
            throw new ArgumentOutOfRangeException(nameof(count));

        var read = WrapIndex(_read);

        if (IsContiguous(index, count))
        {
            var i = Array.IndexOf(_buf, item, WrapIndex(read + index), count);
            return i == -1 ? i : WrapIndex(i - read);
        }
        else
        {
            var leftLen = Capacity - read;
            var leftCount = leftLen - index;
            var rightCount = count - leftCount;

            var i = Array.IndexOf(_buf, item, read + index, leftCount);
            if (i != -1)
                return i - read;

            var j = Array.IndexOf(_buf, item, 0, rightCount);
            if (j != -1)
                return j + leftLen;

            return -1;
        }
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (IsFull) Grow();

        if (index <= Count - index)
        {
            // Closer to r.
            WrapCopy(_read, _read - 1, index);
            _read--;
        }
        else
        {
            // Closer to w.
            WrapCopy(_read + index, _read + index + 1, Count - index);
            _write++;
        }

        this[index] = item;
    }

    public int LastIndexOf(T item)
    {
        return LastIndexOf(item, Count - 1);
    }

    public int LastIndexOf(T item, int index)
    {
        return LastIndexOf(item, index, index + 1);
    }

    public int LastIndexOf(T item, int index, int count)
    {
        if (Count == 0)
            return -1;

        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || index < count - 1)
            throw new ArgumentOutOfRangeException(nameof(count));

        int ToLeftIndex(int index, int count)
        {
            return index - count + 1;
        }

        int ToRightIndex(int index, int count)
        {
            return index + count - 1;
        }

        var read = WrapIndex(_read);

        index = ToLeftIndex(index, count);

        if (IsContiguous(index, count))
        {
            var i = Array.LastIndexOf(_buf, item, WrapIndex(read + ToRightIndex(index, count)), count);
            return i == -1 ? i : WrapIndex(i - read);
        }
        else
        {
            var leftLen = Capacity - read;
            var leftCount = leftLen - index;
            var rightCount = count - leftCount;

            var j = Array.LastIndexOf(_buf, item, ToRightIndex(0, rightCount), rightCount);
            if (j != -1)
                return j + leftLen;

            var i = Array.LastIndexOf(_buf, item, read + ToRightIndex(index, leftCount), leftCount);
            if (i != -1)
                return i - read;

            return -1;
        }
    }

    public T PeekFirst()
    {
        if (IsEmpty)
            throw new InvalidOperationException();

        return this[0];
    }

    public T PeekLast()
    {
        if (IsEmpty)
            throw new InvalidOperationException();

        return this[Count - 1];
    }

    public T PopFirst()
    {
        var item = PeekFirst();
        RemoveFirst();
        return item;
    }

    public T PopLast()
    {
        var item = PeekLast();
        RemoveLast();
        return item;
    }

    public bool Remove(T item)
    {
        var i = IndexOf(item);
        if (i == -1)
            return false;

        RemoveAt(i);
        return true;
    }

    public void RemoveAt(int index)
    {
        RemoveRange(index, 1);
    }

    public void RemoveFirst()
    {
        if (IsEmpty)
            throw new InvalidOperationException();

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            this[0] = default!;

        _read++;
        _version++;
    }

    public void RemoveLast()
    {
        if (IsEmpty)
            throw new InvalidOperationException();

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            this[Count - 1] = default!;

        _write--;
        _version++;
    }

    public void RemoveRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || count > Count - index)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (index <= Count - (index + count))
        {
            // Closer to r.
            WrapCopy(_read, _read + count, index);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                WrapClear(_read, count);
            _read += count;
        }
        else
        {
            // Closer to w.
            WrapCopy(_read + index + count, _read + index, Count - (index + count));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                WrapClear(_read + Count - count, count);
            _write -= count;
        }

        _version++;
    }

    public void Reverse()
    {
        Reverse(0, Count);
    }

    public void Reverse(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0 || count > Count - index)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (IsContiguous(index, count))
        {
            Array.Reverse(_buf, WrapIndex(_read + index), count);
        }
        else
        {
            MakeContiguous();
            Array.Reverse(_buf, index, count);
        }

        _version++;
    }

    public T[] ToArray()
    {
        var array = new T[Count];
        CopyTo(array);
        return array;
    }

    public void TrimExcess()
    {
        var newCapacity = NextCapacity(Count);
        if (newCapacity == Capacity)
            return;

        Relocate(newCapacity);
        _version++;
    }

    public bool TryPeekFirst(out T item)
    {
        if (IsEmpty)
        {
            item = default!;
            return false;
        }

        item = this[0];
        return true;
    }

    public bool TryPeekLast(out T item)
    {
        if (IsEmpty)
        {
            item = default!;
            return false;
        }

        item = this[Count - 1];
        return true;
    }

    public bool TryPopFirst(out T item)
    {
        if (!TryPeekFirst(out item))
            return false;

        RemoveFirst();
        return true;
    }

    public bool TryPopLast(out T item)
    {
        if (!TryPeekLast(out item))
            return false;

        RemoveLast();
        return true;
    }

    private static bool IsCompatibleObject(object? value)
    {
        return value is T || (value == null && default(T) == null);
    }

    private static bool IsNullAndNullsAreIllegal(object? value)
    {
        return value == null && default(T) != null;
    }

    private static int NextCapacity(int n)
    {
        if (n == 0)
            return 0;

        return (int)((uint)n).NextPowerOfTwo();
    }

    private void Grow()
    {
        try
        {
            if (Capacity == 0)
                Relocate(DefaultCapacity);
            else
                Relocate(2 * Capacity);
        }
        catch (OverflowException)
        {
            throw new OutOfMemoryException();
        }
    }

    private bool IsContiguous(int index, int count)
    {
        return count == 0 || WrapIndex(_read + index) <= WrapIndex(_read + index + count - 1);
    }

    private void MakeContiguous()
    {
        Relocate(Capacity);
    }

    private void Relocate(int capacity)
    {
        var newBuf = new T[capacity];
        CopyTo(newBuf);
        _buf = newBuf;
        _write = Count;
        _read = 0;
    }

    private void WrapClear(int index, int count)
    {
        index = WrapIndex(index);

        if (index + count <= Capacity)
        {
            Array.Clear(_buf, index, count);
        }
        else
        {
            Array.Clear(_buf, index, Capacity - index);
            Array.Clear(_buf, 0, count - (Capacity - index));
        }
    }

    private void WrapCopy(int srcIndex, int dstIndex, int count)
    {
        Debug.Assert(count <= Capacity / 2); // (*)

        srcIndex = WrapIndex(srcIndex);
        dstIndex = WrapIndex(dstIndex);

        if (srcIndex <= dstIndex)
        {
            var a = Math.Min(Capacity - dstIndex, count);
            var c = Math.Max(srcIndex + count - Capacity, 0);
            var b = count - (a + c);

            if (srcIndex + count <= dstIndex)
            {
                //  s                               |
                //  +---------------+               |
                //  |       A       |               |
                //  +---------------+               |
                //  |               +---------------+
                //  |               |       A'      |
                //  |               +---------------+
                //  |               d               |

                //  |  s                            |
                //  |  +---------+-----+            |
                //  |  |    A    |  B  |            |
                //  |  +---------+-----+            |
                //  |                     +---------+-----+
                //  |                     |    A'   |  B' |
                //  |                     +---------+-----+
                //  |                     d         |

                // In this case, A' ∩ B = ∅ holds.
                Array.Copy(_buf, srcIndex, _buf, dstIndex, a);
                if (b > 0)
                    Array.Copy(_buf, srcIndex + a, _buf, 0, b);
            }
            else
            {
                // By (*), d + count ≤ s + Capacity.

                //  |   s                           |   s + Capacity
                //  |   +---------------+           |   +---
                //  |   |       A       |           |   |
                //  |   +---------------+           |   +---
                //  |           +---------------+   |
                //  |           |       A'      |   |
                //  |           +---------------+   |
                //  |           d                   |

                //  |           s                   |           s + Capacity
                //  |           +-----------+---+   |           +---
                //  |           |     A     | B |   |           |
                //  |           +-----------+---+   |           +---
                //  |                   +-----------+---+
                //  |                   |     A'    | B'|
                //  |                   +-----------+---+
                //  |                   d           |

                //  |                   s           |                   s + Capacity
                //  |                   +---+-------+---+               +---
                //  |                   | A |   B   | C |               |
                //  |                   +---+-------+---+               +---
                //  |                           +---+-------+---+
                //  |                           | A'|   B'  | C'|
                //  |                           +---+-------+---+
                //  |                           d   |

                // In this case, C' ∩ (A ∪ B) = ∅ and B' ∩ A = ∅ hold.
                if (c > 0)
                    Array.Copy(_buf, 0, _buf, b, c);
                if (b > 0)
                    Array.Copy(_buf, srcIndex + a, _buf, 0, b);
                Array.Copy(_buf, srcIndex, _buf, dstIndex, a);
            }
        }
        else
        {
            var a = Math.Min(Capacity - srcIndex, count);
            var c = Math.Max(dstIndex + count - Capacity, 0);
            var b = count - (a + c);

            if (dstIndex + count <= srcIndex)
            {
                //  |               s               |
                //  |               +---------------+
                //  |               |       A       |
                //  |               +---------------+
                //  +---------------+               |
                //  |       A'      |               |
                //  +---------------+               |
                //  d                               |

                //  |                     s         |
                //  |                     +---------+-----+
                //  |                     |    A    |  B  |
                //  |                     +---------+-----+
                //  |  +---------+-----+            |
                //  |  |    A'   |  B' |            |
                //  |  +---------+-----+            |
                //  |  d                            |

                // In this case, B' ∩ A = ∅ holds.
                if (b > 0)
                    Array.Copy(_buf, 0, _buf, dstIndex + a, b);
                Array.Copy(_buf, srcIndex, _buf, dstIndex, a);
            }
            else
            {
                // By (*), s + count ≤ d + Capacity.

                //  |           s                   |
                //  |           +---------------+   |
                //  |           |       A       |   |
                //  |           +---------------+   |
                //  |   +---------------+           |   +---
                //  |   |       A'      |           |   |
                //  |   +---------------+           |   +---
                //  |   d                           |   d + Capacity

                //  |                   s           |
                //  |                   +-----------+---+
                //  |                   |     A     | B |
                //  |                   +-----------+---+
                //  |           +-----------+---+   |           +---
                //  |           |     A'    | B'|   |           |
                //  |           +-----------+---+   |           +---
                //  |           d                   |           d + Capacity

                //  |                           s   |
                //  |                           +---+-------+---+
                //  |                           | A |   B   | C |
                //  |                           +---+-------+---+
                //  |                   +---+-------+---+               +---
                //  |                   | A'|   B'  | C'|               |
                //  |                   +---+-------+---+               +---
                //  |                   d           |                   d + Capacity

                // In this case, A' ∩ (B ∪ C) = ∅ and B' ∩ C = ∅ hold.
                Array.Copy(_buf, srcIndex, _buf, dstIndex, a);
                if (b > 0)
                    Array.Copy(_buf, 0, _buf, dstIndex + a, b);
                if (c > 0)
                    Array.Copy(_buf, b, _buf, 0, c);
            }
        }
    }

    private int WrapIndex(int index)
    {
        return index & (Capacity - 1);
    }
}

internal static class UInt32Extensions
{
    public static uint NextPowerOfTwo(this uint x)
    {
        if (x == 0)
            return 1;

        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x++;
        return x;
    }
}
