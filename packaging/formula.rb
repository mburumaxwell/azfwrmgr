# typed: false
# frozen_string_literal: true

# name must start with caps and match the tool name (AzureFwrMgr, Azurefwrmgr, do not work)
class Azfwrmgr < Formula
  desc "Azure Firewall Rules Manager."
  homepage "https://github.com/mburumaxwell/azfwrmgr"
  license "MIT"
  version "#{VERSION}#"

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/mburumaxwell/azfwrmgr/releases/download/#{VERSION}#/azfwrmgr-#{VERSION}#-linux-arm64.tar.gz"
      sha256 "#{RELEASE_SHA256_LINUX_ARM64}#"
    end

    if Hardware::CPU.intel?
      url "https://github.com/mburumaxwell/azfwrmgr/releases/download/#{VERSION}#/azfwrmgr-#{VERSION}#-linux-x64.tar.gz"
      sha256 "#{RELEASE_SHA256_LINUX_X64}#"
    end
  end

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/mburumaxwell/azfwrmgr/releases/download/#{VERSION}#/azfwrmgr-#{VERSION}#-osx-arm64.tar.gz"
      sha256 "#{RELEASE_SHA256_MACOS_ARM64}#"
    end

    if Hardware::CPU.intel?
      url "https://github.com/mburumaxwell/azfwrmgr/releases/download/#{VERSION}#/azfwrmgr-#{VERSION}#-osx-x64.tar.gz"
      sha256 "#{RELEASE_SHA256_MACOS_X64}#"
    end
  end

  def install
    bin.install "azfwrmgr"
  end

  test do
    system "#{bin}/azfwrmgr", "--version"
  end
end
