# EchoDotNetLite
ECHONET Liteやその周辺の規格/仕様を.Net Core 2.1で実装したものです

# 概要図

![概要図](image.png?3)


# プロジェクト構成
|プロジェクト名|概要|備考|
|--|--|--|
|SkstackIpDotNet|SKSTACK-IPのAPIラッパーライブラリ<br>RL7023 Stick-D/IPSに付属のSKコマンドリファレンスマニュアル`SKSTACK-IP(Single-hop Edition) Version 1.2.1a`をもとに全コマンドを実装|Bルートで使用しないコマンドのテスト不足<br>一部レスポンス解析未実装<br>|
|EchoDotNetLite|ECHONET Lite 通信ミドルウェアライブラリ<br>ECHONET Lite規格書 Ver.1.13をもとに全サービスを実装|Bルートで使用しないサービスのテスト不足<br>|
|EchoDotNetLite.Specifications|ECHONET機器オブジェクト詳細規定の定義<br>JSONファイル、およびそれを読み取るクラス郡<br>APPENDIX ECHONET機器オブジェクト詳細規定 Release K （日本語版）をもとに生成|APPENDIXからJSONへの変換過程で脱字等が発生している可能性あり|
|EchoDotNetLiteSkstackIpBridge|EchoDotNetLiteとSkStackIPのブリッジクラス||
|EchoDotNetLiteSkstackIpBridge.Example|低圧スマート電力量メータ(Bルート)のコントローラー実装例 コンソールアプリケーション||
|EchoDotNetLiteLANBridge|EchoDotNetLiteといわゆるLANのブリッジクラス|Wi-SUN HANとは異なる|
|EchoDotNetLiteLANBridge.Example|LAN経由で家電を操作する、コントローラー実装例<br>コンソールアプリケーション<br>MoekadenRoomでサポートする機器オブジェクトとの相互通信を実装(EchoDotNetLiteの実装確認が目的)||


* 全般的に異常系処理全般の考慮が不足している

# 動作確認環境
* OS:Windows10/Raspbian Stretch
* ミドルウェア:.NET Core 2.1 Runtime v2.1.6
* Wi-SUNモジュール： [RL7023 Stick-D/IPS](https://www.tessera.co.jp/rl7023stick-d_ips.html)
* 機器オブジェクトエミュレーター:[MoekadenRoom](https://github.com/SonyCSL/MoekadenRoom/blob/master/README.jp.md)
* 低圧スマート電力量メータ:中部電力管内(2機)

# 参考情報
* [HEMS-スマートメーターBルート(低圧電力メーター)運用ガイドライン第4.0版](http://www.meti.go.jp/committee/kenkyukai/shoujo/smart_house/pdf/009_s03_00.pdf)
* [エコーネット規格　（一般公開）](https://echonet.jp/spec_g/)
* [GitHub SkyleyNetworks/SKSTACK_API](https://github.com/SkyleyNetworks/SKSTACK_API)
* [MoekadenRoom (機器オブジェクトエミュレーター)](https://github.com/SonyCSL/MoekadenRoom/blob/master/README.jp.md)
* [OpenECHO (ECHONET Liteのjava実装)](https://github.com/SonyCSL/OpenECHO)
