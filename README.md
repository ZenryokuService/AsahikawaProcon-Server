
# Attention
英語ではない。

# Introduction
　詳細は知らないので大雑把に書く。  
旭川市では毎年、旭川プロコンなるイベントが開催されている。
中学生と高校生あたりを対象にゲーム型の競技プロコンをするって話だ。
ゲームシステムはCHaserと呼ばれるどこかの競技ルールを丸パクリしている。
簡単に言うとプログラマ同士がはAIを作って戦わせるという競技である。
サーバー、クライアント共に言語はJavaで開発され、当然LinuxでもWindowsでもどこでも動く。
なでしこを使ってうんたらとか言う話も聞いたような気がするがここではなかったことにする。  

　2014年に開催された第3か第4回あたりの旭川プロコンではJavaでのAI作成は敷居が高いのではと考えられ、
C言語でもAIを作成できるように、クライアントのC言語のライブラリが作成された。  
サーバーの仕様は変更していないため、なんか釧路が勝手に作ってたRubyのクライアントでもなんでも、
TCP通信の規格を守っていればしっかりと通信できる点が今回のポイントである。  

　そして今回、2015年の旭川プロコンでは運営のお偉いさんの一人がいなくなるのもあって、
旭川プロコンのシステムを大きく変えるにもよいタイミングであること。


# Propose
　2015年の旭川プロコンの変更案として、クロスプラットフォームフレームワークQtを用いてサーバーをC++で書くことを提案する。
Win/Mac/Linuxで動作する上に、アプリケーションとして隔離させてしまえばライブラリのリンクだの言われる心配はなくなる。
そのため各OS向けにあらかじめコンパイル済みのバイナリで配布することが予想される。
過去のサーバーとの後方互換も実現も視野に入れたい。

　クライアントは中学生でも簡単に利用できるHSPを使用する。HSPであってもTCP通信ができることが確認されているため、
サーバーの規約通りに通信することでクライアントをして動作できる。


# Problem
　QtでのプログラミングはC++のフレームワークであるが、独特の構文も存在するため、
数年間このサーバーが使われるとすると、サーバーのメンテナンスが容易とは言えない。
　
　HSPはWindows環境依存であるために、サーバーに後方互換性がないと競技の参加自体がWindowsに縛られてしまう。
かといってHSP以外の言語を許可すると講習会の負担が増える恐れもある。てか私HSPできないし、なんとかしたいところ。



