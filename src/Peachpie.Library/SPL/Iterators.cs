﻿using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Pchp.Core.Reflection;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// The Seekable iterator.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public interface SeekableIterator : Iterator
    {
        /// <summary>
        /// Seeks to a given position in the iterator.
        /// </summary>
        void seek(long position);
    }

    /// <summary>
    /// Classes implementing OuterIterator can be used to iterate over iterators.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public interface OuterIterator : Iterator
    {
        /// <summary>
        /// Returns the inner iterator for the current iterator entry.
        /// </summary>
        /// <returns>The inner <see cref="Iterator"/> for the current entry.</returns>
        Iterator getInnerIterator();
    }

    /// <summary>
    /// Classes implementing RecursiveIterator can be used to iterate over iterators recursively.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public interface RecursiveIterator : Iterator
    {
        /// <summary>
        /// Returns an iterator for the current iterator entry.
        /// </summary>
        /// <returns>An <see cref="RecursiveIterator"/> for the current entry.</returns>
        RecursiveIterator getChildren();

        /// <summary>
        /// Returns if an iterator can be created for the current entry.
        /// </summary>
        /// <returns>Returns TRUE if the current entry can be iterated over, otherwise returns FALSE.</returns>
        bool hasChildren();
    }

    /// <summary>
    /// This iterator allows to unset and modify values and keys while iterating over Arrays and Objects.
    /// 
    /// When you want to iterate over the same array multiple times you need to instantiate ArrayObject
    /// and let it create ArrayIterator instances that refer to it either by using foreach or by calling
    /// its getIterator() method manually.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class ArrayIterator : Iterator, Traversable, ArrayAccess, SeekableIterator, Countable
    {
        #region Fields & Properties

        readonly protected Context _ctx;

        PhpArray _array;
        OrderedDictionary.Enumerator _arrayEnumerator;    // lazily instantiated so we can rewind() once when needed
        bool isArrayIterator => _array != null;

        object _dobj = null;
        IEnumerator<KeyValuePair<IntStringKey, PhpValue>> _dobjEnumerator = null;    // lazily instantiated so we can rewind() once when needed
        bool isObjectIterator => _dobj != null;

        bool _isValid = false;

        /// <summary>
        /// Instantiate new PHP array's enumerator and advances its position to the first element.
        /// </summary>
        /// <returns><c>True</c> whether there is an first element.</returns>
        void InitArrayIteratorHelper()
        {
            Debug.Assert(_array != null);

            _arrayEnumerator = new OrderedDictionary.Enumerator(_array);
            _isValid = _arrayEnumerator.MoveFirst();
        }

        /// <summary>
        /// Instantiate new object's enumerator and advances its position to the first element.
        /// </summary>
        /// <returns><c>True</c> whether there is an first element.</returns>
        void InitObjectIteratorHelper()
        {
            Debug.Assert(_dobj != null);

            _dobjEnumerator = TypeMembersUtils.EnumerateVisibleInstanceFields(_dobj).GetEnumerator();   // we have to create new enumerator (or implement InstancePropertyIterator.Reset)
            _isValid = _dobjEnumerator.MoveNext();
        }

        #endregion

        #region Constructor

        public ArrayIterator(Context/*!*/ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            _ctx = ctx;
        }

        public ArrayIterator(Context/*!*/ctx, PhpValue array, int flags = 0)
            : this(ctx)
        {
            __construct(ctx, array, flags);
        }

        /// <summary>
        /// Constructs the array iterator.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="array">The array or object to be iterated on.</param>
        /// <param name="flags">Flags to control the behaviour of the ArrayIterator object. See ArrayIterator::setFlags().</param>
        public virtual void __construct(Context/*!*/ctx, PhpValue array, int flags = 0)
        {
            if ((_array = array.ArrayOrNull()) != null)
            {
                InitArrayIteratorHelper();  // instantiate now, avoid repetitous checks during iteration
            }
            else if ((_dobj = array.AsObject()) != null)
            {
                //InitObjectIteratorHelper();   // lazily to avoid one additional allocation
            }
            else
            {
                throw new ArgumentException();
                //// throw an PHP.Library.SPL.InvalidArgumentException if anything besides an array or an object is given.
                //Exception.ThrowSplException(
                //    _ctx => new InvalidArgumentException(_ctx, true),
                //    context,
                //    null, 0, null);
            }
        }

        #endregion

        #region ArrayIterator (uasort, uksort, natsort, natcasesort, ksort, asort)

        public virtual void uasort(IPhpCallable cmp_function)
        {
            throw new NotImplementedException();
        }

        public virtual void uksort(IPhpCallable cmp_function)
        {
            throw new NotImplementedException();
        }

        public virtual void natsort()
        {
            throw new NotImplementedException();
        }

        public virtual void natcasesort()
        {
            throw new NotImplementedException();
        }

        public virtual void ksort()
        {
            throw new NotImplementedException();
        }

        public virtual void asort()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ArrayIterator (getFlags, setFlags, append, getArrayCopy)

        public virtual int getFlags()
        {
            throw new NotImplementedException();
        }

        public virtual object setFlags(int flags)
        {
            throw new NotImplementedException();
        }

        public virtual PhpArray getArrayCopy()
        {
            if (isArrayIterator)
                return _array.DeepCopy();

            throw new NotImplementedException();
        }

        public virtual void append(PhpValue value)
        {
            if (isArrayIterator)
            {
                _array.Add(value);
            }
            else if (isObjectIterator)
            {
                // php_error_docref(NULL TSRMLS_CC, E_RECOVERABLE_ERROR, "Cannot append properties to objects, use %s::offsetSet() instead", Z_OBJCE_P(object)->name);
            }
        }

        #endregion

        #region interface Iterator

        public virtual void rewind()
        {
            if (isArrayIterator)
            {
                _isValid = _arrayEnumerator.MoveFirst();
            }
            else if (isObjectIterator)
            {
                // isValid set by InitObjectIteratorHelper()
                InitObjectIteratorHelper(); // DObject enumerator does not support MoveFirst()
            }
        }

        private void EnsureEnumeratorsHelper()
        {
            if (isObjectIterator && _dobjEnumerator == null)
                InitObjectIteratorHelper();

            // arrayEnumerator initialized in __construct()
        }

        public virtual void next()
        {
            if (isArrayIterator)
            {
                _isValid = _arrayEnumerator.MoveNext();
            }
            else if (isObjectIterator)
            {
                EnsureEnumeratorsHelper();
                _isValid = _dobjEnumerator.MoveNext();
            }
        }

        public virtual bool valid()
        {
            EnsureEnumeratorsHelper();
            return _isValid;
        }

        public virtual PhpValue key()
        {
            EnsureEnumeratorsHelper();

            if (_isValid)
            {
                if (isArrayIterator)
                    return _arrayEnumerator.CurrentKey;
                else if (isObjectIterator)
                    return PhpValue.Create(_dobjEnumerator.Current.Key);
                else
                    Debug.Fail(null);
            }

            return PhpValue.Null;
        }

        public virtual PhpValue current()
        {
            EnsureEnumeratorsHelper();

            if (_isValid)
            {
                if (isArrayIterator)
                    return _arrayEnumerator.CurrentValue;
                else if (isObjectIterator)
                    return _dobjEnumerator.Current.Value;
                else
                    Debug.Fail(null);
            }

            return PhpValue.Null;
        }

        #endregion

        #region interface ArrayAccess

        public virtual PhpValue offsetGet(PhpValue index)
        {
            if (isArrayIterator)
            {
                return _array.GetItemValue(index);
            }
            //else if (isObjectIterator)
            //    return _dobj[index];

            return PhpValue.False;
        }

        public virtual void offsetSet(PhpValue index, PhpValue value)
        {
            if (isArrayIterator)
            {
                if (index != null) _array.Add(index, value);
                else _array.Add(value);
            }
            //else if (isObjectIterator)
            //{
            //    _dobj.Add(index, value);
            //}
        }

        public virtual void offsetUnset(PhpValue index)
        {
            throw new NotImplementedException();
        }

        public virtual bool offsetExists(PhpValue index)
        {
            if (isArrayIterator)
            {
                return index.TryToIntStringKey(out var iskey) && _array.ContainsKey(iskey);
            }
            //else if (isObjectIterator)
            //    return _dobj.Contains(index);

            return false;
        }

        #endregion

        #region interface SeekableIterator

        public void seek(long position)
        {
            int currentPosition = 0;

            if (position < 0)
            {
                //
            }

            this.rewind();

            while (this.valid() && currentPosition < position)
            {
                this.next();
                currentPosition++;
            }
        }

        #endregion

        #region interface Countable

        public virtual long count()
        {
            if (isArrayIterator)
                return _array.Count;
            else if (isObjectIterator)
                return TypeMembersUtils.FieldsCount(_dobj);

            return 0;
        }

        #endregion

        #region interface Serializable

        public virtual PhpString serialize()
        {
            throw new NotImplementedException();
        }

        public virtual void unserialize(PhpString data)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// The EmptyIterator class for an empty iterator.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class EmptyIterator : Iterator, Traversable
    {
        public virtual void __construct()
        {
        }

        #region interface Iterator

        public void rewind()
        {
        }

        public void next()
        {
        }

        public virtual bool valid() => false;

        public virtual PhpValue key()
        {
            Debug.Fail("not implemented");
            //Exception.ThrowSplException(
            //    _ctx => new BadMethodCallException(_ctx, true),
            //    context,
            //    CoreResources.spl_empty_iterator_key_access, 0, null);
            return PhpValue.Null;
        }

        public virtual PhpValue current()
        {
            Debug.Fail("not implemented");
            //Exception.ThrowSplException(
            //    _ctx => new BadMethodCallException(_ctx, true),
            //    context,
            //    CoreResources.spl_empty_iterator_value_access, 0, null);
            return PhpValue.Null;
        }

        #endregion
    }

    /// <summary>
    /// This iterator wrapper allows the conversion of anything that is Traversable into an Iterator.
    /// It is important to understand that most classes that do not implement Iterators have reasons
    /// as most likely they do not allow the full Iterator feature set.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class IteratorIterator : OuterIterator
    {
        /// <summary>
        /// Object to iterate on.
        /// </summary>
        private Traversable _iterator;

        /// <summary>
        /// Enumerator over the <see cref="iterator"/>.
        /// </summary>
        protected IPhpEnumerator _enumerator;

        /// <summary>
        /// Wheter the <see cref="_enumerator"/> is in valid state (initialized and not at the end).
        /// </summary>
        protected bool _valid = false;

        [PhpFieldsOnlyCtor]
        protected IteratorIterator() { }

        public IteratorIterator(Traversable iterator, string classname = null) => __construct(iterator, classname);

        public virtual void __construct(Traversable iterator, string classname = null)
        {
            if (classname != null)
            {
                //var downcast = ctx.ResolveType(PhpVariable.AsString(classname), null, this.iterator.TypeDesc, null, ResolveTypeFlags.ThrowErrors);

                PhpException.ArgumentValueNotSupported(nameof(classname), classname);
                throw new NotImplementedException();
            }

            _iterator = iterator;

            if (iterator is Iterator)
            {
                // ok
            }
            else
            {
                PhpException.InvalidArgument(nameof(iterator));
            }

            //rewind(context);  // not in PHP, performance reasons (foreach causes rewind() itself)
        }

        public virtual Iterator getInnerIterator()
        {
            return (Iterator)_iterator;
        }

        public virtual void rewind()
        {
            if (_iterator != null)
            {
                // we can make use of standard foreach enumerator
                _enumerator = Operators.GetForeachEnumerator(_iterator, true, default(RuntimeTypeHandle));

                //
                _valid = _enumerator.MoveNext();
            }
        }

        public virtual void next()
        {
            // init iterator first (this skips the first element as on PHP)
            if (_enumerator == null)
            {
                rewind();
            }

            // enumerator can be still null, if iterator is null
            _valid = _enumerator != null && _enumerator.MoveNext();
        }

        public virtual bool valid()
        {
            return _valid;
        }

        public virtual PhpValue key()
        {
            return (_enumerator != null && _valid) ? _enumerator.CurrentKey : PhpValue.Void;
        }

        public virtual PhpValue current()
        {
            return (_enumerator != null && _valid) ? _enumerator.CurrentValue : PhpValue.Void;
        }

        // TODO: hide this method to not be visible by PHP code, make this behaviour internal
        //public virtual PhpValue __call(ScriptContext context, object name, object args)
        //{
        //    var methodname = PhpVariable.AsString(name);
        //    var argsarr = args as PhpArray;

        //    if (this.iterator == null || argsarr == null)
        //    {
        //        PhpException.UndefinedMethodCalled(this.TypeName, methodname);
        //        return null;
        //    }

        //    // call the method on internal iterator, as in PHP,
        //    // only PHP leaves $this to self (which is potentionally dangerous and not correctly typed)
        //    context.Stack.AddFrame((ICollection)argsarr.Values);
        //    return this.iterator.InvokeMethod(methodname, null, context);
        //}
    }

    /// <summary>
    /// This abstract iterator filters out unwanted values.
    /// This class should be extended to implement custom iterator filters.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public abstract class FilterIterator : IteratorIterator
    {
        [PhpFieldsOnlyCtor]
        protected FilterIterator() { }

        public FilterIterator(Traversable iterator) : base(iterator)
        {
        }

        void SkipNotAccepted()
        {
            if (_enumerator != null)
            {
                // skip not accepted elements
                while (_valid && !this.accept())
                {
                    _valid = _enumerator.MoveNext();
                }
            }
        }

        /// <summary>
        /// Returns whether the current element of the iterator is acceptable through this filter.
        /// </summary>
        public abstract bool accept();
    }


    ///// <summary>
    ///// This abstract iterator filters out unwanted values for a RecursiveIterator.
    ///// This class should be extended to implement custom filters.
    ///// </summary>
    //[PhpType(PhpTypeAttribute.InheritName)]
    //public abstract class RecursiveFilterIterator : FilterIterator, RecursiveIterator
    //{
    //    [PhpFieldsOnlyCtor]
    //    protected RecursiveFilterIterator() { }

    //    public RecursiveFilterIterator(RecursiveIterator iterator) => __construct(iterator);

    //    public sealed override void __construct(Traversable iterator, string classname = null) => base.__construct(iterator, classname);

    //    public virtual void __construct(RecursiveIterator iterator) => __construct((Iterator)iterator);
        
    //    public RecursiveIterator getChildren()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool hasChildren()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}