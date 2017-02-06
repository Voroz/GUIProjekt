using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt 
{
    class CircularStack<T> 
    {
        /******************************************************
         CALL: CircularStack<T> myStack;
         TASK: Uses an array of a certain datatype.
         *****************************************************/
        public CircularStack(uint capacity) 
        {
            _usedSize = 0;
            _capacity = capacity;
            _front = 0;
            _arr = new T[_capacity];
        }

        /******************************************************
         CALL: push(T val);
         TASK: Pushes the parameter onto the stack.
         *****************************************************/
        public void push(T val) 
        {
            if (_usedSize != _capacity) {
                _usedSize++;
            }
            _arr[_front] = val;
            _front = (_front + 1) % _capacity;
        }

        /******************************************************
         CALL: pop();
         TASK: The latest element added gets deleted.
         *****************************************************/
        public void pop() 
        {
            Debug.Assert(_usedSize != 0);
            if (_front == 0) {
                _front = _capacity;
            }
            _front--;
            _usedSize--;
        }

        /******************************************************
         CALL: T topVal = top();
         TASK: Returns the value on top of the stack.
         *****************************************************/
        public T top() 
        {
            Debug.Assert(_usedSize != 0);
            uint toReturn = _front;
            if (toReturn == 0) {
                toReturn = _capacity;
            }
            toReturn--;
            return _arr[toReturn];
        }

        /******************************************************
         CALL: int size = size();
         TASK: Returns the number of elements on the stack.
         *****************************************************/
        public uint size() 
        {
            return _usedSize;
        }


        private T[] _arr;
        private uint _usedSize;
        private uint _capacity;
        private uint _front;
    }
}
