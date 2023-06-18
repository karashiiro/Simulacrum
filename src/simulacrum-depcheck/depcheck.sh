#!/usr/bin/env bash

set -eo pipefail

assert_dependency() {
    type -P "$1" || (echo "$1 is not installed, please install it!" && exit 1)
}

assert_dependency "yarn"
assert_dependency "docker"
assert_dependency "python3"

echo
echo "All environment dependencies are installed!"
