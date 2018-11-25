# SkstackIpDotNet
SKSTACK IP .NET Library

# 動作確認環境
* Wi-SUNモジュール：テセラ・テクノロジーのHEMS用 Wi-SUNモジュール
  Wi-SUN Route-B 専用 [RL7023 Stick-D/IPS](https://www.tessera.co.jp/rl7023stick-d_ips.html)
* OS:Windows10/Raspbian Stretch

# 依存関係
* シリアルポートの処理に[SerialPortStream](https://github.com/jcurl/SerialPortStream)を使用しています。
* Linux上で動作させる場合、[こちら](https://github.com/jcurl/SerialPortStream#linux)に記載の通り、サポート・ライブラリー`libnserial.so`をコンパイルする必要があります

# 実装状況
* RL7023 Stick-D/IPSに付属のSKコマンドリファレンスマニュアル
`SKSTACK-IP(Single-hop Edition) Version 1.2.1a`をもとに全コマンド実装

# 参考情報
* [Skyley Networks/Bルートやってみた](http://www.skyley.com/products/b-route.html)
* [GitHub SkyleyNetworks/SKSTACK_API](https://github.com/SkyleyNetworks/SKSTACK_API)