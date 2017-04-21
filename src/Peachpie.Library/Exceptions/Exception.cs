﻿using System;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
    /// </summary>
    [PhpType("[name]")]
    public class Exception : System.Exception, Throwable
    {
        protected string message;
        protected long code;
        protected string file;
        protected int line;

        [PhpFieldsOnlyCtor]
        protected Exception() { }

        public Exception(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }

        public void __construct(string message = "", long code = 0, Throwable previous = null)
        {
            this.message = message;
            this.code = code;
        }

        public virtual int getCode() => (int)this.code;

        public virtual string getFile() => this.file;

        public virtual int getLine() => this.line;

        public virtual string getMessage() => this.message;

        public virtual Throwable getPrevious()
        {
            throw new NotImplementedException();
        }

        public virtual PhpArray getTrace()
        {
            throw new NotImplementedException();
        }

        public virtual string getTraceAsString()
        {
            throw new NotImplementedException();
        }

        public virtual string __toString()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Exception thrown if an error which can only be found on runtime occurs.
    /// </summary>
    [PhpType("[name]")]
    public class RuntimeException : Spl.Exception
    {
        [PhpFieldsOnlyCtor]
        protected RuntimeException() { }

        public RuntimeException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }
    }
}