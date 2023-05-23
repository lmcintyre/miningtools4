#!/bin/python

import zlib
import sys

with open(sys.argv[1], "rb") as rsf:
    data = zlib.compress(rsf.read(), 9)
with open(sys.argv[2], "wb") as rsf_out:
    rsf_out.write(data[:64])