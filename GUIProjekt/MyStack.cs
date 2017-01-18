using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    class MyStack<T>
    {
        public MyStack(T[] arr) {
		    _arr = arr;
	    }

        public void push(T val) {
            Debug.Assert(_usedSize != _arr.Length);
            _arr[_arr.Length - ++_usedSize] = val;
        }
        public void pop() {
            Debug.Assert(_usedSize != 0);
            _usedSize--;
        }
        public T top() {
            Debug.Assert(_usedSize != 0);
            return _arr[_arr.Length - _usedSize--];
        }
        public int size() {
            return _usedSize;
        }

        private T[] _arr;
	    private int _usedSize;
    }
}
