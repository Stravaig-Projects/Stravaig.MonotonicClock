#!/bin/sh

set -e

git config --global --add safe.directory /workspaces/$REPO_NAME

gem install bundler
bundle install --gemfile=/workspaces/$REPO_NAME/docs/Gemfile

bash --version
pwsh --version

echo Gem Version:
gem --version

jekyll --version
bundle --version