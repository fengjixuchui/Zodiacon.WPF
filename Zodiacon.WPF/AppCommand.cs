﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.WPF {
    public interface IAppCommand {
        string Name { get; }
        string Description { get; }
        void Undo();
        void Execute();
    }

    public class AppCommand<T> : IAppCommand {
        T _context;
        Action<T> _execute, _undo;

        public string Description { get; set; }

        public string Name { get; set; }

        public AppCommand(T context, Action<T> execute, Action<T> undo = null) {
            if(execute == null)
                throw new ArgumentNullException(nameof(execute));

            _context = context;
            _execute = execute;
            _undo = undo ?? execute;
        }

        public void Execute() {
            _execute(_context);
        }

        public void Undo() {
            _undo(_context);
        }
    }

}
