CC = gcc
CFLAGS = -g -fPIC -Wall
#LUADIR = /usr/local/include
LUADIR = ../../../../skynet/3rd/lua
TARGET = protobuf.so

.PHONY : all clean

all : $(TARGET)

$(TARGET) : pbc-lua53.c
	$(CC) $(CFLAGS) -shared -o $@ -I../.. -I$(LUADIR) -L../../build $^ -lpbc

clean :
	rm -f $(TARGET)
