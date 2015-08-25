HttpClient
====
HttpClient is an asynchronous Http library for .NET that based on socket and the TAP(Task-based Asynchronous Pattern) pattern.

This library inspired by HttpWebRequest and HttpClient(.NET 4.5),so if you familiar with its,you can immediately start. 

Installation
====

Features
====
- The fast and the lower memory cost and GC collection
- The Connection pool of HTTP for reused
- The Saea(SocketAsyncEventArgs) pool and the Buffer pool(default 4M Cache)
- Supports following HTTP methods: `GET`,~~`POST`,`HEAD`,`PUT`~~
- Supports DNS round-robin algorithm for each of HTTP request
- Supports Cookies manager feature and HTTP content auto-decompression

Usage
====

###HttpRequest####


Tips
====