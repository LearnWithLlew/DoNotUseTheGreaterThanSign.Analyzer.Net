# Project 

This is a .net analyzer that flags the use of the greater than sign (>) in code.
It should also have a auto fixer that replaces the greater than sign with a less than sign (<) and flips values so it's a pure refactoring.
this also includes <=
if multiple numbers are involved it should also order from smaller to larger.
So 
5 > x && x > 2 

should become 

2 < x && x < 5


# Project Structure

Scripts: build_and_test.sh
Unit tests: use xunit
CI: Github actions
    build and test (uses the script) on push
    publish, on github deploy, version from tag (example tag: v1.2.3)
Nuget: publishes the 
