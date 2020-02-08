/*
 * Copyright (c) 2019-2020, Jim Schaad <ietf@augustcellars.com>
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;

namespace Com.AugustCellars.CoAP.OSCOAP
{
    public class OscoapOption : Option
    {
        public OscoapOption() : base(OptionType.Oscoap)
        {

        }

        public OscoapOption(OptionType o) : base(o)
        {
        }

        public void Set(byte[] value) { RawValue = value; }

        public override string ToString()
        {
            if (this.RawValue == null) return "** InPayload";
            return $"** Length={RawValue.Length}";
        }
    }
}
