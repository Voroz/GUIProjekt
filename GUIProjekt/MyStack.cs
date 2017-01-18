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
        /******************************************************
         CALL: MyStack<T> myStack;
         TASK: Uses an array of a certain datatype.
         *****************************************************/
        public MyStack(T[] arr) {
		    _arr = arr;
	    }

        /******************************************************
         CALL: push(T val);
         TASK: Pushes the parameter onto the stack.
         *****************************************************/
        public void push(T val) {
            Debug.Assert(_usedSize != _arr.Length);
            _arr[_arr.Length - ++_usedSize] = val;
        }

        /******************************************************
         CALL: pop();
         TASK: The latest element added gets deleted.
         *****************************************************/
        public void pop() {
            Debug.Assert(_usedSize != 0);
            _usedSize--;
        }

        /******************************************************
         CALL: T topVal = top();
         TASK: Returns the value on top of the stack.
         *****************************************************/
        public T top() {
            Debug.Assert(_usedSize != 0);
            return _arr[_arr.Length - _usedSize--];
        }

        /******************************************************
         CALL: int stackSize = size();
         TASK: Returns the number of elements on the stack.
         *****************************************************/
        public int size() {
            return _usedSize;
        }

        private T[] _arr;
	    private int _usedSize;
    }
}
