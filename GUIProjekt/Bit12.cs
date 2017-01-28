using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUIProjekt
{
    /******************************************************
     A class containing a set of operator overloads to
     enable usage of 12 bit object in the project.
     *****************************************************/
    public class Bit12
    {
        public Bit12(short val) {
            _bit12Val = convertTo12Bit(val);
        }

        public static Bit12 operator +(Bit12 a, Bit12 b)
        {
            Bit12 newBit12 = new Bit12((short)(a.value() + b.value()));
            return newBit12;
        }

        public static Bit12 operator -(Bit12 a, Bit12 b)
        {
            Bit12 newBit12 = new Bit12((short)(a.value() - b.value()));
            return newBit12;
        }

        public static Bit12 operator *(Bit12 a, Bit12 b) {
            Bit12 newBit12 = new Bit12((short)(a.value() * b.value()));
            return newBit12;
        }

        public static Bit12 operator /(Bit12 a, Bit12 b) {
            Bit12 newBit12 = new Bit12((short)(a.value() / b.value()));
            return newBit12;
        }

        public static bool operator ==(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() == rhs.value();
        }

        public static bool operator !=(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() != rhs.value();
        }

        public static bool operator <(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() < rhs.value();
        }

        public static bool operator >(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() > rhs.value();
        }

        public static bool operator <=(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() <= rhs.value();
        }

        public static bool operator >=(Bit12 lhs, Bit12 rhs)
        {
            return lhs.value() <= rhs.value();
        }

        public short value()        {
            return _bit12Val;
        }

        private short convertTo12Bit(short val)
        {

            while (val > 2047)
            {
                val -= 4096;
            }
            while (val < -2048)
            {
                val += 4096;
            }

            return val;
        }

        private short _bit12Val;
    }
}
