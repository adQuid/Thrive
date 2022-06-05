Code Style Guide
================

This fork of Thrive is not linted, and never will be. I firmly believe that the software industry spends far too much time looking at problems in code that are obvious, and not enough time on problems that are important. I could not care less if you want to use single quotes half the time, and indent every third line with spaces instead of tabs. I want to see code that is maintainable. If you choose to open a PR on this repository, I will review it by hand, roughly using the following criteria.

* Is it [SOLID Design](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)? If you want to win an argument with me, cite one of these princibles.
* Would your code make sense to someone who doesn't remember the context in which it was written?
* Does your code make promises, or imply promises, it can't keep?
* Will your code break if something else is changed in a different file?
