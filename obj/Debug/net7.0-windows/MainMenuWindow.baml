   M S B A M L   `   `   `  ����G  CSpaceShooter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:SpaceShooterSpaceShooter  5      !    SpaceShooter.MainMenuWindow   5      W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ,localclr-namespace:SpaceShooter     5      $	��450��5      $	��800��6   .��5
      � ��5   
   �� 5        �� ImageSource$6  0pack://application:,,,/images/menubackground.png��6    j� Stretch$ UniformToFill=�6X   6r    � 	Resources 5   
    �� 
TargetType �� Property �� Value �� Triggers P� Property P� Value��%
  '��       �� 5      " ��6   .�5      ��  � ��$ 60��6+   68   �� 5       � ��$	 125��6*   68   �� 5       � ��$ 20(�6-   6:   �� 5       � ��$ Light"�6/   6?   �� 5       � ��$
 Nugo)�6/   6>   �� 5       � ��
 �����6?   �� 5       � ��
 ���� 6E   �� 5       � ��
 ���� 6F    5      P� 5       �@J�IsMouseOver$
 True��65   . �5      ��  � ��
 ����6L   �� 5       � ��
 ���� 6M   5      5       5!      .+�5$   
   �� $	��Top=�6   $
��Left=�6-   $	��180��6H   $	��125��6U   $��100,100,0,0q�6a   .+�5%      �� -   5        $��
Start Game��5%      6*   �� 5&      -   5        $��Settings��5&      6(   �� 5'      -   5        $
��Quit��5'      6$   5(      �� 5*   
   -   5        $��text_block_title��5*      $��Space Shooter��6,   
�������$��Medium"�6T   $��40(�6h   $	��Top=�6v   $
��Left=�6�   $��	30,30,0,0q�6�   6�   �� 5+   
   -   5        $��text_block_version��5+      $��Version:��6.   
�������$��Italic$�6Q   $��Bottom=�6d   $
��Left=�6   $��	10,0,0,10q�6�   6�   5,      5.      