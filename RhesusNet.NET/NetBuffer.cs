using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace RhesusNet.NET
{
    public class NetBuffer
    {
        const int kBufferResizeOverAllocateBytes = 4;
        byte[] m_internalBuffer;
		uint m_internalBufferSize;
		int m_internalBitPos;
		
		bool m_isReadOnly;
		bool m_sent;

        public bool Sent
        {
            get { return m_sent; }
            set { m_sent = value; }
        }

        public NetBuffer()
        {
	        construct(null, 0);
        }

        public NetBuffer(int bufferDefaultSize)
        {
	        construct(null, bufferDefaultSize);
        }

        public NetBuffer(byte[] buff, int len)
        {
	        construct(buff, len);
        }

        void construct(byte[] buff, int size)
        {
	        m_internalBitPos = 0;
	        m_internalBuffer = null;

            if (size > 0)
            {
                m_internalBuffer = new byte[size];

                if (buff != null)
                    Array.Copy(buff, m_internalBuffer, size);
            }

	        m_internalBufferSize = Convert.ToUInt32(size);
	
	        m_isReadOnly = buff != null;
	        m_sent = false;
        }

        public void Write(byte c)
        {
	        InternalWriteByte(c, 8);
        }

        public void WriteRaw(byte[] b, int length)
        {
            InternalWriteBytes(b, length);
        }

        public void Write(byte[] c, int length)
        {
	        InternalWriteInteger((ulong)length, sizeof(ushort)*8);
	        InternalWriteBytes(c, length);
        }

        public void Write(string str)
        {
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();

            Write(asciiEncoding.GetBytes(str), str.Length);
        }

        public void Write(bool b)
        {
	        InternalWriteByte(Convert.ToByte(b ? 1 : 0), 1);
        }

        public void Write(double d)
        {
            UInt64 packed = NetUtil.PackDouble(d);

            Write(packed);
        }

        public void Write(float f)
        {
            uint packed = NetUtil.PackFloat(f);

            Write(packed);
        }

        public void Write(long l)
        {
            InternalWriteInteger((ulong)l, sizeof(long) * 8);
        }

        public void Write(ulong l)
        {
            InternalWriteInteger(l, sizeof(ulong) * 8);
        }

        public void Write(int i)
        {
	        InternalWriteInteger((ulong)i, sizeof(int)*8);
        }

        public void Write(uint i)
        {
            InternalWriteInteger((ulong)i, sizeof(uint) * 8);
        }

        public void Write(short s)
        {
	        InternalWriteInteger((ulong)s, sizeof(short)*8);
        }

        void WritePadBits()
        {	
	        if(m_isReadOnly)
	        {
		        //AsyncPrinter::Println("[NetBuffer] Can't write to a read-only buffer!");
		        return;
	        }

            m_internalBitPos += 8 - m_internalBitPos % 8;
        }

        public byte ReadByte()
        {
	        return InternalReadByte(8);
        }

        public byte[] ReadBytes()
        {
	        ushort len = (ushort)InternalReadInteger(sizeof(ushort) * 8);
	
	        return InternalReadBytes(len);
        }

        public string ReadString()
        {
	        ushort len = (ushort)InternalReadInteger(sizeof(ushort) * 8);

            return Encoding.Default.GetString(InternalReadBytes(len));
        }

        public ulong ReadUInt64()
        {
            return InternalReadInteger(sizeof(ulong) * 8);
        }

        public long ReadInt64()
        {
            return (long)InternalReadInteger(sizeof(long) * 8);
        }

        public uint Readuint()
        {
            return (uint)InternalReadInteger(sizeof(uint) * 8);
        }

        public int ReadInt32()
        {
	        return (int)InternalReadInteger(sizeof(int) * 8);
        }

        public short ReadInt16()
        {
	        return (short)InternalReadInteger(sizeof(short) * 8);
        }

        public double ReadDouble()
        {
            UInt64 packed = ReadUInt64();

            return NetUtil.UnpackDouble(packed);
        }

        public float ReadFloat()
        {
            uint packed = Readuint();

            return NetUtil.UnpackFloat(packed);
        }

        public bool ReadBool()
        {
	        return InternalReadByte(1) == 1;
        }

        void SkipPadBits()
        {
	        m_internalBitPos += 8 - m_internalBitPos % 8;
        }

        bool AssertBufferHasSpace(uint bits)
        {
	        return ((bits + 7) >> 3) <= m_internalBufferSize;
        }

        void InternalWriteByte(byte data, int bit_length)
        {
	        if(m_isReadOnly)
	        {
		        //AsyncPrinter::Println("[NetBuffer] Can't write to a read-only buffer!");
		        return;
	        }
	
	        if(bit_length < 1 || bit_length > 8)
	        {
		        //AsyncPrinter::Println("[NetBuffer] Can't write less than one bit or more than eight bits!");
		        return;
	        }
	
	        FitBufferToSize((uint)(GetBytePos() * 8 + bit_length));
	
	        int bit_pos = GetBitIndexInCurrentByte();
	
	        // this operation performs a logical AND on the given data and the bit_length
	        // in order to get rid of the unnecessary bits.
	        // The data is AND'ed with 0xFF (1111 1111) shifted to the right by 8 - bit_length, creating the masker.
            byte data_masked = (byte)(((uint)data & ((uint)(~(uint)(0)) >> (8 - bit_length))) << (8 - bit_length));
	
	        int remainingBits = 8 - bit_pos;
	        int overflow = bit_length - remainingBits;

            int pos = GetBytePos();

            m_internalBuffer[pos] |= (byte)(data_masked >> ((8 - bit_length) + overflow));
	
	        // this byte is finished
	        if(overflow <= 0)
	        {
	        }
	        // write into the next byte
	        else if(overflow > 0)
	        {
		
		        remainingBits = overflow;
		
		        // mask off written bits
		        data_masked &= (byte)((uint)(~(uint)(0)) >> (8 - remainingBits));
		
		        m_internalBuffer[GetBytePos() + 1] |= (byte)(data_masked << (8 - remainingBits));
	        }

            m_internalBitPos += bit_length;
        }

        void InternalWriteBytes(byte[] data, int bytes)
        {
	        if(m_isReadOnly)
	        {
		        //AsyncPrinter::Println("[NetBuffer] Can't write to a read-only buffer!");
		        return;
	        }
	
	        for(int i = 0; i < bytes; i++)
	        {
		        InternalWriteByte(data[i], 8);
	        }
        }

        void InternalWriteInteger(ulong data, int bits)
        {	
	        if(m_isReadOnly)
	        {
		        //AsyncPrinter::Println("[NetBuffer] Can't write to a read-only buffer!");
		        return;
	        }
	
	        for(int i = 0; i < bits; i += 8)
	        {
		        if(i + 8 > bits)
		        {
			        // this is the last bitset
			        int rem_bits = bits - i;
			        InternalWriteByte((byte)(data >> i), rem_bits);
		        }
		        else
		        {
			        InternalWriteByte((byte)(data >> i), 8);
		        }
	        }
        }

        byte InternalReadByte(int bit_length)
        {
	        if(bit_length < 1 || bit_length > 8)
	        {
                Console.WriteLine("[NetBuffer] Can't read less than one bit or more than eight bits!");
		        return 0;
	        }
	
	        if(!AssertBufferHasSpace(Convert.ToUInt32(m_internalBitPos + bit_length)))
	        {
		        Console.WriteLine("[NetBuffer] Can't read past the buffer!");
		        return 0;
	        }
	
	        int bit_pos = GetBitIndexInCurrentByte();
	
	        int remainingBits = 8 - bit_pos;
	        int overflow = bit_length - remainingBits;

            int pos = GetBytePos();

            uint masker = 0;

            if (overflow < 0)
            {
                masker = ((uint)0xff) << (-overflow);
            }
            else
            {
                masker = ((uint)0xff) >> (overflow);
            }

            uint retrieved = (uint)(((uint)m_internalBuffer[pos]) & masker);

	        if(overflow <= 0)
	        {
		        // we're done.
	        }
	        else if(overflow > 0)
	        {
                retrieved = retrieved << (overflow);
                retrieved = (byte)((uint)(retrieved) | ((m_internalBuffer[GetBytePos() + 1] & (~(uint)(0) << (8 - overflow))) >> (8 - overflow)));
            }
	
	        m_internalBitPos += bit_length;
            return (byte)(retrieved >> (8 - bit_length));
        }

        byte[] InternalReadBytes(int bytes)
        {	
	        // done like this so that the array stays alive after the function returns
            byte[] retrieved = new byte[bytes];

	        for(int i = 0; i < bytes; i++)
	        {
		        retrieved[i] = InternalReadByte(8);
	        }
	
	        return retrieved;
        }

        ulong InternalReadInteger(int bits)
        {	
	        ulong retrieved = 0;
	
	        for(int i = 0; i < bits; i += 8)
	        {
		        if(i + 8 > bits)
		        {
			        // this is the last bitset
			        int rem_bits = bits - i;
			        retrieved |= (ulong)InternalReadByte(rem_bits) << i;
		        }
		        else
		        {
			        retrieved |= (ulong)InternalReadByte(8) << i;
		        }
	        }
	
	        return retrieved;
        }

        // TO-DO change these to properties
        public int GetBufferLength()
        {
            return (int)((m_internalBitPos + 7) / 8);
        }

        public int GetBytePos()
        {
	        return (int)(m_internalBitPos / 8);
        }

        public int GetBitIndexInCurrentByte()
        {
	        return m_internalBitPos % 8;
        }

        public byte[] GetBuffer()
        {
            return m_internalBuffer;
        }

        void FitBufferToSize(uint bits)
        {
	        uint bytes = (bits + 7) >> 3;
	
	        if(m_internalBuffer == null)
	        {
		        int len = (int)(bytes + kBufferResizeOverAllocateBytes);
		        m_internalBuffer = new byte[len];
		        m_internalBufferSize = (uint)len;
	        }
	        else if(bytes > m_internalBufferSize)
	        {
		        int len = (int)bytes + kBufferResizeOverAllocateBytes;
		        byte[] newBuff = new byte[len];
		
		        Array.Copy(m_internalBuffer,newBuff, m_internalBufferSize);
		        m_internalBufferSize = (uint)len;
		
		        //delete[] m_internalBuffer;
		
		        m_internalBuffer = newBuff;
	        }
        }
    }

        
}
